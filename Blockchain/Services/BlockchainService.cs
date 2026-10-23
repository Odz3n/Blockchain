using Blockchain.Menus;
using Blockchain.Models;
using Blockchain.Models.Metrics;
using Blockchain.Models.Validation;
using System.Diagnostics;
using System.Text.Json;

namespace Blockchain.Services;

public class BlockchainService
{
    private readonly HashService _hashService;
    private readonly MiningService _miningService;
    private readonly TransactionService _transactionService;
    private readonly BalanceService _balanceService;
    private readonly StorageService _storageService;
    private readonly NetworkService _networkService;
    private readonly NodeService _nodeService;

    private readonly MenuContext _menuContext;
    public List<Block> Chain { get; set; } = new();
    public List<Transaction> MemPool { get; set; } = new();
    private readonly int _maxMemPoolCount = 20;

    public int Difficulty { get; set; } = 4;

    private readonly double _targetBlockTime = 2;
    private readonly int _adjustmentInterval = 2;
    private readonly int _minDifficulty = 1;
    private readonly int _maxDifficulty = 6;

    private int _maxTransactionsPerBlock = 5;
    public decimal InitialMiningReward { get; private set; } = 64m;
    private int _halvingInterval = 4;
    private int _minimumReward = 2;

    public BlockchainService(
        NetworkService networkService,
        MenuContext menuContext,
        int difficulty = 1,
        double targetBlockTime = 2,
        int adjustmentInterval = 2)
    {
        _hashService = new();
        _miningService = new();
        _transactionService = new();
        _balanceService = new();
        _storageService = new();
        _nodeService = new(this, networkService);

        _adjustmentInterval = adjustmentInterval;
        _targetBlockTime = targetBlockTime;

        Difficulty = difficulty;

        _menuContext = menuContext;
        _networkService = networkService;
        _networkService.Start(async (type, message) =>
        {
            _menuContext.SetStatus($"Received message of type {type}.");
            switch (type)
            {
                case "Transaction":
                    {
                        var transaction = JsonSerializer.Deserialize<Transaction>(message);
                        if (transaction != null)
                        {
                            _menuContext.SetStatus($"Received transaction from {transaction.From} to {transaction.To} for amount {transaction.Amount}.");
                            var addingResult = AddReceivedTransaction(transaction);
                            _menuContext.SetStatus(addingResult.Message, addingResult.IsValid);
                        }
                        break;
                    }
                case "Block":
                    {
                        var block = JsonSerializer.Deserialize<Block>(message);
                        if (block != null)
                        {
                            _menuContext.SetStatus($"Received block with index {block.Index} and hash {block.Hash}");
                            var addingResult = AddReceivedBlock(block);
                            _menuContext.SetStatus(addingResult.Message, addingResult.IsValid);
                        }
                        break;
                    }
                case "GET_CHAIN":
                    {
                        _menuContext.SetStatus($"Received GET_CHAIN message.");
                        await _networkService.BroadcastAsync(type: "CHAIN", new BlockchainState
                        {
                            Chain = Chain,
                            MemPool = MemPool,
                            Difficulty = Difficulty
                        });
                    }
                    break;
                case "CHAIN":
                    {
                        var state = JsonSerializer.Deserialize<BlockchainState>(message);
                        var result = TryReplaceChain(state);
                        if (!result.IsValid)
                        {
                            _menuContext.SetStatus($"Failed replacing chain: {result.Message}", result.IsValid);
                            return;
                        }
                        _menuContext.SetStatus("Chain replaced successfully.", result.IsValid);
                        return;
                    }
                case "GET_NODE_STATS":
                    {
                        _menuContext.SetStatus($"Received GET_NODE_STATS message.");
                        
                        var request = JsonSerializer.Deserialize<NodeStatsRequest>(message);
                        if (request == null)
                        {
                            _menuContext.SetStatus("Invalid node statistics request.", false);
                            break;
                        }

                        var statistics = _nodeService.GetNodeStatistics();

                        await _networkService.SendAsync(
                            ip: request.IP,
                            port: request.Port,
                            type: "NODE_STATS",
                            data: statistics);

                        _menuContext.SetStatus($"Node statistics sent to {request.IP}:{request.Port}.");

                        break;
                    }
                case "NODE_STATS":
                    {
                        var statistics = JsonSerializer.Deserialize<NodeStatistics>(message);

                        if (statistics == null)
                        {
                            _menuContext.SetStatus("Invalid node statistics response.", false);
                            break;
                        }

                        _menuContext.SetStatus(
                            $"Node Statistics | " +
                            $"Blocks: {statistics.Blocks:N0} | " +
                            $"Transactions: {statistics.TransactionsInBlockchain:N0} | " +
                            $"Mempool: {statistics.MempoolCount:N0} | " +
                            $"Pending Fees: {statistics.PendingFees:N4} | " +
                            $"Avg. Mining: {statistics.AverageMiningTime:g} | " +
                            $"Reward: {statistics.CurrentMiningReward:N4} | " +
                            $"Next Halving Block #: {statistics.NextHalvingBlock:N0} | " +
                            $"Peers: {statistics.PeersCount:N0}",
                            true);

                        break;
                    }
                default:
                    _menuContext.SetStatus($"Received data of unsupported type.");
                    break;
            }
        });

        AddGenesisBlock();
    }
    public int GetNextHalvingBlock()
    {
        var currentIndex = Chain[Chain.Count - 1].Index;
        var halvings = currentIndex / _halvingInterval;
        var left = (halvings + 1) * _halvingInterval - currentIndex;

        return currentIndex + left;
    }
    public (Block, MiningMetrics) MinePendingTransactions(string minerAddress)
    {
        int expiredCount = RemoveExpiredTransactions();

        var transactions = SelectTransactionsForBlock(_maxTransactionsPerBlock);

        var validTransactions = new List<Transaction>();
        var rejectedTransactions = new List<Transaction>();
        foreach (var transaction in transactions)
        {
            var validationResult = _transactionService.ValidateTransactionWithBalance(
                transaction,
                Chain,
                validTransactions,
                includeExpiryCheck: true);

            if (validationResult.IsValid)
                validTransactions.Add(transaction);
            else
            {
                _menuContext.SetStatus(validationResult.Message, validationResult.IsValid);
                rejectedTransactions.Add(transaction);
            }
        }

        MemPool.RemoveAll(t => rejectedTransactions.Any(r => r.Id == t.Id));

        transactions = validTransactions;

        var totalFee = transactions.Sum(t => t.Fee);

        Block lastBlock = Chain[Chain.Count - 1];

        var newBlock = new Block
        {
            Index = lastBlock.Index + 1,
            Transactions = new List<Transaction>(transactions),
            Author = minerAddress,
            PrevHash = lastBlock.Hash,
            Difficulty = Difficulty
        };

        // Current mining reward + sum of transactions' fee
        var miningReward = GetMinerReward(newBlock.Index);
        var expectedReward = miningReward + totalFee;

        var coinbase = _transactionService.CreateCoinbaseTransaction(minerAddress, expectedReward);
        newBlock.Transactions.Insert(0, coinbase);
        newBlock.MerkleRoot = _hashService.ComputeMerkleRoot(newBlock.Transactions);

        var miningMetrics = _miningService.MineBlock(newBlock, Difficulty);

        Chain.Add(newBlock);

        MemPool.RemoveAll(t => transactions.Any(v => v.Id == t.Id));

        miningMetrics.AcceptedTransactions = transactions.Count;
        miningMetrics.RejectedTransactions = rejectedTransactions.Count + expiredCount;
        miningMetrics.TotalFees = totalFee;
        miningMetrics.MiningReward = miningReward;
        miningMetrics.RemainingMempoolTransactions = MemPool.Count;

        if (newBlock.Index % _adjustmentInterval == 0)
            AdjustDifficulty();

        return (newBlock, miningMetrics);
    }
    private int RemoveExpiredTransactions()
    {
        var expiredTransactions = MemPool
            .Where(_transactionService.IsExpired)
            .ToList();

        foreach (var transaction in expiredTransactions)
        {
            MemPool.Remove(transaction);
            _menuContext.SetStatus($"Expired: {transaction.Id}", false);
        }

        return expiredTransactions.Count;
    }
    public Transaction AddTransaction(
        Transaction transaction,
        bool includeValidation = true)
    {
        if (MemPool.Count >= _maxMemPoolCount)
            throw new ArgumentException($"Mempool full. ({MemPool.Count}/{_maxMemPoolCount})");

        if (MemPool.Any(t => t.Id == transaction.Id))
            throw new ArgumentException($"Duplicate: Transaction {transaction.Id} is already in the pool.");

        if (includeValidation)
        {
            var validationResult = _transactionService.ValidateTransactionWithBalance(transaction, Chain, MemPool, includeExpiryCheck: true);
            if (!validationResult.IsValid)
                throw new ArgumentException($"Invalid transaction: {validationResult.Message}");
        }

        if (Chain.Any(b => b.Transactions.Any(t => t.Id == transaction.Id)))
            throw new ArgumentException(
                $"Duplicate: Transaction {transaction.Id} is already confirmed.");


        MemPool.Add(transaction);

        return MemPool.ToList()[^1];
    }
    public bool CancelTransaction(
        string transactionId,
        string senderAddress)
    {
        var senderTransactions = MemPool
            .Where(t => t.From == senderAddress)
            .ToList();

        if (senderTransactions.Count <= 0)
            throw new KeyNotFoundException($"There are no active transactions by {senderAddress}.");

        var transaction = senderTransactions
            .Where(t => t.Id == transactionId)
            .FirstOrDefault()
            ?? throw new KeyNotFoundException($"Transaction {transactionId} by {senderAddress} not found.");

        return MemPool.Remove(transaction);
    }
    public List<Transaction> SelectTransactionsForBlock(int maxCount)
    {
        return MemPool
            .Where(t => t.Fee >= 1)
            .OrderByDescending(t => t.Fee)
            .ThenBy(t => t.Timestamp)
            .Take(maxCount)
            .ToList();
    }
    public void ChangeTransactionAmount(
        int blockIndex,
        string transactionId,
        decimal amount)
    {
        var block = Chain
            .FirstOrDefault(b => b.Index == blockIndex)
            ?? throw new KeyNotFoundException("Block not found.");

        var transaction = block.Transactions
            .FirstOrDefault(t => t.Id == transactionId)
            ?? throw new KeyNotFoundException("Transaction not found.");

        //if (amount <= 0)
        //    throw new ArgumentOutOfRangeException(nameof(amount));

        transaction.Amount = amount;
    }
    private void AdjustDifficulty()
    {
        var recentBlocks = Chain
            .Where(b => b.Index > 0)
            .TakeLast(_adjustmentInterval)
            .ToList();

        if (recentBlocks.Count == 0)
            throw new InvalidOperationException(nameof(recentBlocks.Count));

        var avgTime = recentBlocks.Average(b => b.MiningDuration);

        //var metrics = new DifficultyChangeMetrics
        //{
        //    TargetBlockTime = _targetBlockTime,
        //    AvgMiningTime = avgTime,
        //    OldDifficulty = Difficulty
        //};

        if (avgTime < _targetBlockTime * 0.25)
        {
            ChangeDifficulty(Math.Min(_maxDifficulty, Difficulty + 2));
            //metrics.Reason = "Average mining time below 25% of target.";
        }
        else if (avgTime < _targetBlockTime * 0.5)
        {
            ChangeDifficulty(Math.Min(_maxDifficulty, Difficulty + 1));
            //metrics.Reason = "Average mining time below 50% of target.";
        }
        else if (avgTime > _targetBlockTime * 4)
        {
            ChangeDifficulty(Math.Max(_minDifficulty, Difficulty - 2));
            //metrics.Reason = "Average mining time above 400% of target.";
        }
        else if (avgTime > _targetBlockTime * 2)
        {
            ChangeDifficulty(Math.Max(_minDifficulty, Difficulty - 1));
            //metrics.Reason = "Average mining time above 200% of target.";
        }
        //else
        //{
        //    metrics.Reason = "Nothing changed.";
        //}

        //metrics.NewDifficulty = Difficulty;
        //return metrics;
    }
    public ValidationResult Validate(List<Block> chain)
    {
        if (chain[0].Hash != Chain[0].Hash)
            return new ValidationResult
            {
                IsValid = false,
                Message = "Received chain has invalid Genesis Block's Hash."
            };
        var transactionIds = new HashSet<string>();
        foreach (var transaction in chain.SelectMany(b => b.Transactions))
        {
            if (!transactionIds.Add(transaction.Id))
                return new ValidationResult
                {
                    IsValid = false,
                    Message = $"Duplicate transaction ID: {transaction.Id}."
                };
        }

        var historicalDifficulty = chain
            .Take(Chain.Count)
            .Sum(b => b.Difficulty);
        var localDifficulty = Chain.Sum(b => b.Difficulty);

        if (historicalDifficulty != localDifficulty)
            return new ValidationResult
            {
                IsValid = false,
                Message = "Historical difficulty mismatch."
            };

        for (int i = 1; i < chain.Count; i++)
        {
            var currentBlock = chain[i];
            var prevBlock = chain[i - 1];

            string currentHash = _hashService.ComputeHash(currentBlock);

            if (currentBlock.Hash != currentHash)
                return new ValidationResult
                {
                    IsValid = false,
                    BlockIndex = currentBlock.Index,
                    Message = "Invalid block hash."
                };

            if (prevBlock.Hash != currentBlock.PrevHash)
                return new ValidationResult
                {
                    IsValid = false,
                    BlockIndex = currentBlock.Index,
                    Message = "Invalid block prevHash."
                };

            var target = new string('0', currentBlock.Difficulty);
            if (!currentBlock.Hash.StartsWith(target))
                return new ValidationResult 
                {
                    IsValid = false,
                    BlockIndex = currentBlock.Index,
                    Message = $"Invalid block Hash Mask."
                };

            if (currentBlock.MerkleRoot != _hashService.ComputeMerkleRoot(currentBlock.Transactions))
                return new ValidationResult
                {
                    IsValid = false,
                    BlockIndex = currentBlock.Index,
                    Message = "Invalid MarkleRoot hash."
                };
        }
        return new ValidationResult
        {
            IsValid = true,
            Message = "Blockchain is valid."
        };
    }
    public ValidationResult ValidateEconomy(
        bool includeIntegrityValidation = true,
        bool includeSignatureValidation = true,
        bool includeExpiryCheck = false)
    {
        if (includeIntegrityValidation)
        {
            var integrityResult = Validate(Chain);
            if (!integrityResult.IsValid)
                return integrityResult;
        }

        var balances = new Dictionary<string, decimal>();
        foreach (var block in Chain)
        {
            if (block.Index == 0)
            {
                if (block.Transactions.Count != 0)
                    return new ValidationResult
                    {
                        IsValid = false,
                        BlockIndex = block.Index,
                        Message = "Genesis block must be empty."
                    };

                continue;
            }

            var regularResult = ValidateRegularTransactions(block, balances, includeSignatureValidation, includeExpiryCheck);
            if (!regularResult.IsValid)
                return regularResult;

            var expectedReward = GetMinerReward(block.Index) + block.Transactions
                .Where(t => !t.IsCoinbase && t.Type != Models.Type.Coinbase)
                .Sum(t => t.Fee);

            var coinbaseResult = _transactionService.ValidateCoinbase(block, expectedReward);
            if (!coinbaseResult.IsValid)
                return coinbaseResult;

            var coinbase = block.Transactions.First(t => t.IsCoinbase);
            _balanceService.ApplyCoinbase(coinbase, balances);
        }
        return new ValidationResult
        {
            IsValid = true,
            Message = "Blockchain economy is valid."
        };
    }
    private ValidationResult ValidateRegularTransactions(
        Block block,
        Dictionary<string, decimal> balances,
        bool includeSignatureValidation = true,
        bool includeExpiryCheck = false)
    {
        foreach (var transaction in block.Transactions
            .Where(t => !t.IsCoinbase && t.Type != Models.Type.Coinbase))
        {
            // Check transaction
            var transactionResult = _transactionService.ValidateTransaction(transaction, includeSignatureValidation, includeExpiryCheck: includeExpiryCheck);
            if (!transactionResult.IsValid)
                return new ValidationResult
                {
                    IsValid = false,
                    BlockIndex = block.Index,
                    Message = transactionResult.Message
                };

            // Check balance
            var balanceResult = _balanceService.ValidateBalance(transaction, balances);
            if (!balanceResult.IsValid)
                return new ValidationResult
                {
                    IsValid = false,
                    BlockIndex = block.Index,
                    Message = balanceResult.Message
                };

            // Apply historical balance
            _balanceService.ApplyTransaction(transaction, balances);
        }
        return new ValidationResult
        {
            IsValid = true,
            BlockIndex = block.Index,
            Message = "Regular transactions are valid."
        };
    }
    public ValidationResult ValidateMiningRewards()
    {
        foreach (var block in Chain.Skip(1))
        {
            var fees = block.Transactions
                .Where(t => !t.IsCoinbase && t.Type != Models.Type.Coinbase)
                .Sum(t => t.Fee);

            var expectedReward = GetMinerReward(block.Index);

            var expectedCoinbase = expectedReward + fees;

            var actualCoinbase = block.Transactions
                .FirstOrDefault(t => t.IsCoinbase && t.Type == Models.Type.Coinbase)
                ?? throw new KeyNotFoundException($"Coinbase transaction for block #{block.Index} not found.");

            if (expectedCoinbase != actualCoinbase.Amount)
                return new ValidationResult
                {
                    IsValid = false,
                    BlockIndex = block.Index,
                    Message = $"Block #{block.Index}; Expected Reward: {expectedReward}; Fees: {fees}; Expected Coinbase: {expectedCoinbase}; Actual Coinbase: {actualCoinbase}; Status: INVALID"
                };
        }
        return new ValidationResult
        {
            IsValid = true,
            Message = "Mining rewards are valid"
        };
    }
    public List<KeyValuePair<string, bool>> GenerateMerkleProof(Block block, string transactionId)
    {
        if (block == null)
            throw new ArgumentNullException(nameof(block));
        if (string.IsNullOrWhiteSpace(transactionId))
            throw new ArgumentNullException(nameof(transactionId));

        var targetTransaction = block.Transactions
            .FirstOrDefault(t => t.Id == transactionId)
            ?? throw new KeyNotFoundException($"Target transaction {transactionId} not found.");

        var targetIndex = block.Transactions.IndexOf(targetTransaction);
        var hashes = block.Transactions
            .Select(t => _hashService.ComputeSHA256(t.ToRawString()))
            .ToList();

        var siblings = new List<KeyValuePair<string, bool>>();
        while(hashes.Count > 1)
        {
            if (hashes.Count % 2 != 0)
                hashes.Add(hashes[hashes.Count - 1]);

            var siblingIndex = (targetIndex % 2 == 0) ? targetIndex + 1 : targetIndex - 1;
            siblings.Add(new KeyValuePair<string, bool>(hashes[siblingIndex], siblingIndex % 2 == 0));

            var upperLevel = new List<string>();
            for (int i = 0; i < hashes.Count; i += 2)
                upperLevel.Add(_hashService.ComputeSHA256(hashes[i] + hashes[i + 1]));

            hashes = upperLevel;
            targetIndex /= 2;
        }
        return siblings;
    }
    public ValidationResult VerifyMerkleProof(Transaction transaction, List<KeyValuePair<string, bool>> proof, string merkleRoot)
    {
        if (transaction == null)
            throw new ArgumentNullException(nameof(transaction));
        if (proof == null)
            throw new ArgumentNullException(nameof(proof));
        if (string.IsNullOrEmpty(merkleRoot))
            throw new ArgumentNullException(nameof(merkleRoot));

        var computedHash = _hashService.ComputeSHA256(transaction.ToRawString());
        foreach (var item in proof)
        {
            // item.Value = true means that sibling is on the left
            computedHash = item.Value
                ? _hashService.ComputeSHA256(item.Key + computedHash)
                : _hashService.ComputeSHA256(computedHash + item.Key);
        }

        if (merkleRoot != computedHash)
            return new ValidationResult
            {
                IsValid = false,
                Message = "Invalid Merkle Root"
            };
        return new ValidationResult
        {
            IsValid = true,
            Message = "Merkle Root is Valid"
        };
    }
    public ValidationResult AddReceivedBlock(Block block)
    {
        var lastBlock = Chain[^1];
        if (block.PrevHash != lastBlock.Hash)
            return new ValidationResult
            {
                IsValid = false,
                BlockIndex = block.Index,
                Message = $"Invalid Block: PrevHash does not match."
            };
        if (block.Index != lastBlock.Index + 1)
            return new ValidationResult
            {
                IsValid = false,
                BlockIndex = block.Index,
                Message = "Invalid Block: Index is not sequential."
            };
        if (block.Hash != _hashService.ComputeHash(block))
            return new ValidationResult
            {
                IsValid = false,
                BlockIndex = block.Index,
                Message = "Invalid Block: Hash is incorrect."
            };
        if (block.MerkleRoot != _hashService.ComputeMerkleRoot(block.Transactions))
            return new ValidationResult
            {
                IsValid = false,
                BlockIndex = block.Index,
                Message = "Invalid Block: Merkle Root is incorrect."
            };
        if (!block.Hash.StartsWith(new string('0', block.Difficulty)))
            return new ValidationResult
            {
                IsValid = false,
                BlockIndex = block.Index,
                Message = "Invalid Block: Hash does not meet difficulty requirements."
            };

        var transactionIds = block.Transactions
            .Where(t => !t.IsCoinbase && t.Type != Models.Type.Coinbase)
            .Select(t => t.Id)
            .ToList();
        MemPool.RemoveAll(t => transactionIds.Contains(t.Id));

        Chain.Add(block);
        return new ValidationResult
        {
            IsValid = true,
            BlockIndex = block.Index,
            Message = $"Block #{block.Index} added successfully."
        };
    }
    public ValidationResult AddReceivedTransaction(Transaction transaction)
    {
        // Skip if already added
        if (MemPool.Any(t => t.Id == transaction.Id))
            return new ValidationResult
            {
                IsValid = true,
                Message = $"Duplicate: Transaction {transaction.Id} is already in the pool."
            };
        if (Chain.Any(b => b.Transactions.Any(t => t.Id == transaction.Id)))
            throw new ArgumentException(
                $"Duplicate: Transaction {transaction.Id} is already confirmed.");

        // Validation
        var transactions = new List<Transaction>(MemPool);
        var validationResult = _transactionService.ValidateTransactionWithBalance(transaction, Chain, transactions, includeExpiryCheck: true);
        if (!validationResult.IsValid)
            return new ValidationResult
            {
                IsValid = false,
                Message = $"Invalid transaction received. {validationResult.Message}"
            };

        // Adding to the pool, excluding validation because already passed
        var newTransaction = AddTransaction(transaction, includeValidation: false);
        return new ValidationResult
        {
            IsValid = true,
            Message = $"Transaction {newTransaction.Id} added successfully."
        };
    }
    public async Task SaveAsync()
    {
        await _storageService.SaveAsync(this);
    }
    public async Task<BlockchainState> LoadAsync()
    {
        return await _storageService.LoadAsync<BlockchainState>(Path.Combine(Directory.GetCurrentDirectory(), "save.json"));
    }
    public ValidationResult RestoreState(BlockchainState state)
    {
        Chain = state.Chain;
        MemPool = state.MemPool;
        Difficulty = state.Difficulty;

        return new ValidationResult
        {
            IsValid = true,
            Message = "Blockchain state restored successfully."
        };
    }
    public ValidationResult TryReplaceChain(BlockchainState state)
    {
        ArgumentNullException.ThrowIfNull(state);

        if (state.Chain.Count <= Chain.Count)
            return new ValidationResult
            {
                IsValid = false,
                Message = "Received chain is not longer than the local chain."
            };
        if (state.Chain[Chain.Count - 1].Hash != Chain[^1].Hash)
            return new ValidationResult
            {
                IsValid = false,
                Message = "Received chain does not extend the local blockchain."
            };

        var validationResult = Validate(state.Chain);
        if (!validationResult.IsValid)
        {
            _menuContext.SetStatus(validationResult.Message, validationResult.IsValid);
            return validationResult;
        }

        var confirmedTransactionIds = state.Chain
            .SelectMany(b => b.Transactions)
            .Select(t => t.Id)
            .ToHashSet();

        var pendingTransactions = MemPool
            .Concat(state.MemPool)
            .Where(t => !confirmedTransactionIds.Contains(t.Id))
            .GroupBy(t => t.Id)
            .Select(g => g.First())
            .OrderByDescending(t => t.Fee)
            .ThenBy(t => t.Timestamp)
            .ToList();

        var validTransactions = new List<Transaction>();
        foreach (var transaction in pendingTransactions)
        {
            var result = _transactionService.ValidateTransactionWithBalance(
                transaction,
                state.Chain,
                validTransactions,
                includeExpiryCheck: true);

            if (result.IsValid)
                validTransactions.Add(transaction);
        }

        Chain = state.Chain;
        Difficulty = state.Difficulty;
        MemPool = validTransactions;

        return new ValidationResult
        {
            IsValid = true,
            Message = "Chain replaced successfully. Mempool merged and revalidated."
        };
    }
    public ChainMetrics GetChainMetrics()
    {
        double minMiningDuration = Chain.Min(b => b.MiningDuration);
        double maxMiningDuration = Chain.Max(b => b.MiningDuration);
        int maxDiff = Chain.Max(b => b.Difficulty);
        int minDiff = Chain.Min(b => b.Difficulty);
        long attemptsCount = Chain.Max(b => b.Nonce);

        return new ChainMetrics
        {
            FastestBlock = Chain.FirstOrDefault(b => b.MiningDuration <= minMiningDuration),
            SlowestBlock = Chain.FirstOrDefault(b => b.MiningDuration >= maxMiningDuration),
            MostAttemptsBlock = Chain.FirstOrDefault(b => b.Nonce >= attemptsCount),
            AvgMiningTime = Chain.Average(b => b.MiningDuration),
            AvgAttemptsCount = Chain.Average(b => b.Nonce),
            MaxDiffAtMining = maxDiff,
            MinDiffAtMining = minDiff
        };
    }
    public void ChangeDifficulty(int difficulty)
    {
        if (difficulty < 0 || difficulty > 64)
            throw new ArgumentOutOfRangeException(nameof(difficulty));

        Difficulty = difficulty;
    }
    //public void ChangeData(Block? block, string? data)
    //{
    //    ArgumentNullException.ThrowIfNull(block);
    //    ArgumentException.ThrowIfNullOrWhiteSpace(data);

    //    if (!Chain.Contains(block))
    //        throw new ArgumentException("Block does not belong to this blockchain.", nameof(block));

    //    block.Data = data;

    //    RemineBlock(block);
    //}
    public MiningMetrics RemineBlock(Block? block)
    {
        ArgumentNullException.ThrowIfNull(block);

        int index = Chain.IndexOf(block);

        if (index < 0 || index > Chain.Count - 1)
            throw new ArgumentException("Block does not belong to this blockchain.", nameof(block));

        if (index > 0)
            block.PrevHash = Chain[index - 1].Hash;

        block.Nonce = default;

        return _miningService.MineBlock(block, block.Difficulty);
    }
    public ChainRepairMetrics RepairChain(int startIndex = 0)
    {
        if (startIndex < 0 || startIndex >= Chain.Count)
            throw new ArgumentOutOfRangeException(nameof(startIndex));

        var metrics = new ChainRepairMetrics();
        var stopwatch = Stopwatch.StartNew();

        for (int i = startIndex; i < Chain.Count; i++)
        {
            var miningMetrics = RemineBlock(Chain[i]);

            metrics.BlocksRepaired++;
            metrics.TotalAttempts += miningMetrics.Attempts;
            metrics.TotalMiningTime += miningMetrics.ElapsedTime;
        }

        stopwatch.Stop();

        metrics.TotalElapsedTime = stopwatch.Elapsed;

        return metrics;
    }
    private void AddGenesisBlock()
    {
        var genesis = new Block
        {
            Index = 0,
            Transactions = new List<Blockchain.Models.Transaction>(),
            Timestamp = DateTime.Parse("1900-01-01"),
            Author = "0",
            PrevHash = "0"
        };

        _miningService.MineBlock(genesis, Difficulty);

        Chain.Add(genesis);
    }
    public decimal GetMinerReward(int blockIndex)
    {
        if (blockIndex <= 0)
            throw new ArgumentOutOfRangeException(nameof(blockIndex));

        int halvings = (blockIndex - 1) / _halvingInterval;

        // Clamped reward after halving
        return Math.Clamp(
            InitialMiningReward / (decimal)Math.Pow(2, halvings),
            _minimumReward,
            InitialMiningReward);
    }
    public decimal GetTotalCoinsCreated()
    {
        decimal totalCoins = 0;
        foreach (var block in Chain.Skip(1))
        {
            var blockTotalFee = block.Transactions
                .Where(t => !t.IsCoinbase && t.Type != Models.Type.Coinbase)
                .Sum(t => t.Fee);

            var coinbase = block.Transactions
                .FirstOrDefault(t => t.IsCoinbase && t.Type == Models.Type.Coinbase)
                    ?? throw new KeyNotFoundException("Coinbase transactions not found.");

            totalCoins += coinbase.Amount - blockTotalFee;
        }
        return totalCoins;
    }
}