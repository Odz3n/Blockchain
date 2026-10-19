using Blockchain.Models;
using Blockchain.Models.Metrics;
using Blockchain.Models.Validation;
using System.Diagnostics;

namespace Blockchain.Services;

public class BlockchainService
{
    private readonly HashService _hashService;
    private readonly MiningService _miningService;
    private readonly TransactionService _transactionService;
    private readonly BalanceService _balanceService;
    public List<Block> Chain { get; set; } = new();
    public List<Transaction> MemPool { get; set; } = new();

    public int Difficulty { get; private set; } = 4;

    private readonly double _targetBlockTime = 2;
    private readonly int _adjustmentInterval = 2;
    private readonly int _minDifficulty = 1;
    private readonly int _maxDifficulty = 6;

    private int _maxTransactionsPerBlock = 3;
    public decimal InitialMiningReward { get; private set; } = 64m;
    private int _halvingInterval = 4;
    private int _minimumReward = 2;

    public BlockchainService(
        int difficulty = 1,
        double targetBlockTime = 2,
        int adjustmentInterval = 2)
    {
        _hashService = new();
        _miningService = new();
        _transactionService = new();
        _balanceService = new();

        _adjustmentInterval = adjustmentInterval;
        _targetBlockTime = targetBlockTime;

        Difficulty = difficulty;

        AddGenesisBlock();
    }
    public DifficultyChangeMetrics? MinePendingTransactions(string minerAddress)
    {
        var transactions = SelectTransactionsForBlock(_maxTransactionsPerBlock);
        var totalFee = transactions.Sum(t => t.Fee);

        Block lastBlock = Chain[Chain.Count - 1];

        var newBlock = new Block
        {
            Index = lastBlock.Index + 1,
            Transactions = transactions,
            Author = minerAddress,
            PrevHash = lastBlock.Hash,
            Difficulty = Difficulty
        };

        // Current mining reward + sum of transactions' fee
        var expectedReward = GetMinerReward(newBlock.Index) + transactions
            .Where(t => !t.IsCoinbase && t.Type != Models.Type.Coinbase)
            .Sum(t => t.Fee);

        var coinbase = _transactionService.CreateCoinbaseTransaction(minerAddress, expectedReward);
        newBlock.Transactions.Insert(0, coinbase);

        _miningService.MineBlock(newBlock, Difficulty);

        Chain.Add(newBlock);

        foreach (var trans in transactions)
            MemPool.Remove(trans);

        if (newBlock.Index % _adjustmentInterval == 0)
            return AdjustDifficulty();

        return null;
    }
    public void AddTransaction(Transaction transaction)
    {
        var validationResult = _transactionService.ValidateTransactionWithBalance(transaction, Chain, MemPool);
        if (!validationResult.IsValid)
            throw new ArgumentException($"Invalid transaction: {validationResult.Message}");

        MemPool.Add(transaction);
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
    private DifficultyChangeMetrics AdjustDifficulty()
    {
        var recentBlocks = Chain
            .Where(b => b.Index > 0)
            .TakeLast(_adjustmentInterval)
            .ToList();

        if (recentBlocks.Count == 0)
            throw new InvalidOperationException(nameof(recentBlocks.Count));

        var avgTime = recentBlocks.Average(b => b.MiningDuration);

        var metrics = new DifficultyChangeMetrics
        {
            TargetBlockTime = _targetBlockTime,
            AvgMiningTime = avgTime,
            OldDifficulty = Difficulty
        };

        if (avgTime < _targetBlockTime * 0.25)
        {
            ChangeDifficulty(Math.Min(_maxDifficulty, Difficulty + 2));
            metrics.Reason = "Average mining time below 25% of target.";
        }
        else if (avgTime < _targetBlockTime * 0.5)
        {
            ChangeDifficulty(Math.Min(_maxDifficulty, Difficulty + 1));
            metrics.Reason = "Average mining time below 50% of target.";
        }
        else if (avgTime > _targetBlockTime * 4)
        {
            ChangeDifficulty(Math.Max(_minDifficulty, Difficulty - 2));
            metrics.Reason = "Average mining time above 400% of target.";
        }
        else if (avgTime > _targetBlockTime * 2)
        {
            ChangeDifficulty(Math.Max(_minDifficulty, Difficulty - 1));
            metrics.Reason = "Average mining time above 200% of target.";
        }
        else
        {
            metrics.Reason = "Nothing changed.";
        }

        metrics.NewDifficulty = Difficulty;

        return metrics;
    }
    public ValidationResult Validate()
    {
        for (int i = 1; i < Chain.Count; i++)
        {
            var currentBlock = Chain[i];
            var prevBlock = Chain[i - 1];

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
        }
        return new ValidationResult
        {
            IsValid = true,
            Message = "Blockchain is valid."
        };
    }
    public ValidationResult ValidateEconomy(
        bool includeIntegrityValidation = true,
        bool includeSignatureValidation = true)
    {
        if (includeIntegrityValidation)
        {
            var integrityResult = Validate();
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

            var regularResult = ValidateRegularTransactions(block, balances, includeSignatureValidation);
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
        bool includeSignatureValidation = true)
    {
        foreach (var transaction in block.Transactions
            .Where(t => !t.IsCoinbase && t.Type != Models.Type.Coinbase))
        {
            // Check transaction
            var transactionResult = _transactionService.ValidateTransaction(transaction, includeSignatureValidation);
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
    private decimal GetCurrentMinerReward()
    {
        return GetMinerReward(Chain.Count);
    }
    private decimal GetMinerReward(int blockIndex)
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