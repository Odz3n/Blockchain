using Blockchain.Menus.Interfaces;
using Blockchain.Models;
using Blockchain.Services;
using Blockchain.UI;
using TransactionType = Blockchain.Models.Type;

namespace Blockchain.Menus;

public class BlockchainMenu: IMenu
{
    private readonly BlockchainService _blockchainService;
    private readonly TransactionService _transactionService;
    private readonly WalletService _walletService;
    private readonly DisplayService _displayService;
    private readonly NetworkService _networkService;
    private readonly ConsoleRenderer _renderer;
    private readonly MenuContext _context;
    private readonly NodeService _nodeService;

    public BlockchainMenu(
        BlockchainService blockchainService,
        TransactionService transactionService,
        WalletService walletService,
        DisplayService displayService,
        NetworkService networkService,
        ConsoleRenderer renderer,
        MenuContext context)
    {
        _blockchainService = blockchainService;
        _transactionService = transactionService;
        _walletService = walletService;
        _displayService = displayService;
        _networkService = networkService;
        _renderer = renderer;
        _context = context;

        _nodeService = new(blockchainService, networkService);
    }

    public async Task Run()
    {
        while (true)
        {
            Console.Clear();
            ShowMenu();

            switch (Console.ReadLine())
            {
                case "1":
                    ChangeDifficulty();
                    break;
                case "2":
                    await AddBlock();
                    break;
                case "3":
                    ShowChain();
                    break;
                case "4":
                    Validate();
                    break;
                case "5":
                    Remine();
                    break;
                case "6":
                    Fix();
                    break;
                case "7":
                    _context.ToggleMetrics();
                    break;
                case "8":
                    ShowChainMetrics();
                    break;
                case "9":
                    ChangeTransactionAmount();
                    break;
                case "10":
                    ValidateEconomy();
                    break;
                case "11":
                    ValidateMiningRewards();
                    break;
                case "12":
                    DisplayMerkleTree();
                    break;
                case "13":
                    VerifyMerkleProof();
                    break;
                case "14":
                    await Save();
                    break;
                case "15":
                    await Sync();
                    break;
                case "16":
                    await GetNodeStats();
                    break;
                case "0":
                    return;
                default:
                    _context.SetStatus("Invalid menu option.", false);
                    break;
            }
        }
    }

    private async Task GetNodeStats()
    {
        Console.Clear();
        _renderer.PrintHeader("NODE STATS");

        _renderer.PrintInfo("0. Local Node", $"127.0.0.1:{_networkService.Port}");

        var nodes = _networkService.Nodes
            .Where(n => !(n.IP == "127.0.0.1" && n.Port == _networkService.Port))
            .ToList();

        for (int i = 0; i < nodes.Count; i++)
        {
            var node = nodes[i];

            _renderer.PrintInfo($"{i + 1}. Node", $"{node.IP}:{node.Port}");
        }

        _renderer.WriteColored("\n  Select Node (0 for local): ", ConsoleColor.Cyan);

        if (!int.TryParse(Console.ReadLine(), out int selection) || selection < 0 || selection > nodes.Count)
        {
            _context.SetStatus("Invalid node selection.", false);
            return;
        }

        if (selection == 0)
        {
            _displayService.DisplayNodeStatistics(_nodeService.GetNodeStatistics());
            Console.ReadKey(true);
            return;
        }

        var selectedNode = nodes[selection - 1];

        try
        {
            await _networkService.SendAsync(
                ip: selectedNode.IP,
                port: selectedNode.Port,
                type: "GET_NODE_STATS",
                data: new NodeStatsRequest
                {
                    IP = "127.0.0.1",
                    Port = _networkService.Port
                });

            _context.SetStatus($"Node statistics requested from {selectedNode.IP}:{selectedNode.Port}.");
        }
        catch (Exception ex)
        {
            _context.SetStatus($"Failed to request node statistics: {ex.Message}", false);
        }
    }

    private async Task Sync()
    {
        await _networkService.BroadcastAsync(type: "GET_CHAIN", "");
    }

    private async Task Save()
    {
        await _blockchainService.SaveAsync();
        _context.SetStatus("Saved!");
    }

    private void VerifyMerkleProof()
    {
        Console.Clear();

        _renderer.PrintHeader("MERKLE PROOF");

        _renderer.WriteColored("\n  Block ID: ", ConsoleColor.Cyan);
        if (!int.TryParse(Console.ReadLine(), out int blockId))
        {
            _context.SetStatus("Invalid block id.", false);
            return;
        }
        var block = _blockchainService.Chain
            .FirstOrDefault(b => b.Index == blockId);
        if (block == null)
        {
            _context.SetStatus($"Block with index '{blockId}' not found.", false);
            return;
        }

        _renderer.WriteColored("\n  Transaction ID: ", ConsoleColor.Cyan);
        string? transactionId = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(transactionId))
        {
            _context.SetStatus("Transaction's Id cannot be null or empty.", false);
            return;
        }
        var transaction = block.Transactions
            .FirstOrDefault(t => t.Id == transactionId);
        if (transaction == null)
        {
            _context.SetStatus($"Transaction with id '{transactionId}' not found.", false);
            return;
        }

        try
        {
            var proof = _blockchainService.GenerateMerkleProof(block, transactionId);
            var validationResult = _blockchainService.VerifyMerkleProof(transaction, proof, block.MerkleRoot);
            _context.SetStatus(validationResult.Message, validationResult.IsValid);
            return;
        }
        catch (ArgumentNullException ex)
        {
            _context.SetStatus(ex.Message, false);
            return;
        }
        catch (KeyNotFoundException ex)
        {
            _context.SetStatus(ex.Message, false);
            return;
        }
        catch (Exception ex)
        {
            _context.SetStatus(ex.Message, false);
            return;
        }
    }

    private void DisplayMerkleTree()
    {
        Console.Clear();
        _renderer.WriteColored("\n  Block ID: ", ConsoleColor.Cyan);
        if (!int.TryParse(Console.ReadLine(), out int blockId))
        {
            _context.SetStatus("Invalid block id.", false);
            return;
        }
        var block = _blockchainService.Chain
            .FirstOrDefault(b => b.Index == blockId);
        if (block == null)
        {
            _context.SetStatus($"Block with index '{blockId}' not found.", false);
            return;
        }

        _displayService.DisplayMerkleTree(block.Transactions);

        _renderer.WaitForKey();
    }

    private void ValidateMiningRewards()
    {
        try
        {
            var validationResult = _blockchainService.ValidateMiningRewards();
            _context.SetStatus(validationResult.Message, validationResult.IsValid);
        }
        catch (KeyNotFoundException ex)
        {
            _context.SetStatus(ex.Message, false);
        }
        catch (Exception ex)
        {
            _context.SetStatus(ex.Message, false);
        }
    }
    private void ValidateEconomy()
    {
        Console.Clear();
        _renderer.PrintHeader("VALIDATE ECONOMY");

        _renderer.WriteColored("\n  Include Integrity Validation (Y/n)?: ", ConsoleColor.Cyan);
        string? includeIntegrity = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(includeIntegrity))
        {
            _context.SetStatus("Answer cannot be null or empty.", false);
            return;
        }
        _renderer.WriteColored("\n  Include Signature Validation (Y/n)?: ", ConsoleColor.Cyan);
        string? includeSignature = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(includeSignature))
        {
            _context.SetStatus("Answer cannot be null or empty.", false);
            return;
        }

        try
        {
            var validationResult = _blockchainService.ValidateEconomy(
                includeIntegrity.ToLowerInvariant() == "y" ? true : false,
                includeSignature.ToLowerInvariant() == "y" ? true : false);
            if (!validationResult.IsValid)
            {
                _context.SetStatus($"At Block #{validationResult.BlockIndex}: {validationResult.Message}", validationResult.IsValid);
                return;
            }
            _context.SetStatus($"{validationResult.Message}", validationResult.IsValid);
            return;
        }
        catch (Exception ex)
        {
            _context.SetStatus(ex.Message, false);
            return;
        }
    }

    private void ShowMemPoolState()
    {
        Console.Clear();
        _renderer.PrintHeader("MEM POOL STATE");

        if (_blockchainService.MemPool.Count == 0)
        {
            _context.SetStatus("MemPool is empty.", true);
            return;
        }

        _displayService.DisplayMemPoolState(_blockchainService.MemPool);
        _renderer.WaitForKey();
    }

    private void ChangeTransactionAmount()
    {
        Console.Clear();
        _renderer.PrintHeader("CHANGE TRANSACTION AMOUNT");

        // Get block
        _renderer.WriteColored("\n  Block ID: ", ConsoleColor.Cyan);
        if (!int.TryParse(Console.ReadLine(), out int blockId))
        {
            _context.SetStatus("Invalid block id.", false);
            return;
        }
        var block = _blockchainService.Chain
            .FirstOrDefault(b => b.Index == blockId);
        if (block == null)
        {
            _context.SetStatus($"Block with index '{blockId}' not found.", false);
            return;
        }

        // Get transaction
        _renderer.WriteColored("\n  Transaction ID: ", ConsoleColor.Cyan);
        string? transactionId = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(transactionId))
        {
            _context.SetStatus("Transaction's Id cannot be null or empty.", false);
            return;
        }
        var transaction = block.Transactions
            .FirstOrDefault(t => t.Id == transactionId);
        if (transaction == null)
        {
            _context.SetStatus($"Transaction with id '{transactionId}' not found.", false);
            return;
        }

        // New amount
        _renderer.PrintInfo("Current amount", transaction.Amount.ToString());
        _renderer.WriteColored("\n  New amount: ", ConsoleColor.Cyan);
        if (!decimal.TryParse(Console.ReadLine(), out decimal newAmount))
        {
            _context.SetStatus("Invalid amount.", false);
            return;
        }
        try
        {
            _blockchainService.ChangeTransactionAmount(blockId, transactionId, newAmount);
            _context.SetStatus($"Transaction's amount changed to {newAmount}.");
        }
        catch (KeyNotFoundException ex)
        {
            _context.SetStatus(ex.Message, false);
            return;
        }
        catch (ArgumentOutOfRangeException ex)
        {
            _context.SetStatus(ex.Message, false);
            return;
        }
    }
    private void ChangeDifficulty()
    {
        Console.Clear();
        _renderer.PrintHeader("CHANGE DIFFICULTY");
        _renderer.PrintInfo("Current", _blockchainService.Difficulty.ToString());

        _renderer.WriteColored("\n  New difficulty: ", ConsoleColor.Cyan);

        if (!int.TryParse(Console.ReadLine(), out int difficulty))
        {
            _context.SetStatus("Invalid difficulty.", false);
            return;
        }
        try
        {
            _blockchainService.ChangeDifficulty(difficulty);
            _context.SetStatus($"Difficulty changed to {difficulty}.");
        }
        catch (ArgumentException ex)
        {
            _context.SetStatus(ex.Message, false);
        }
    }
    private async Task AddBlock()
    {
        while (true)
        {
            Console.Clear();
            _renderer.PrintHeader("ADD BLOCK");
            _renderer.ShowStatusBar();

            Console.WriteLine();

            _renderer.PrintInfo("Transactions in pool", _blockchainService.MemPool.Count.ToString());

            _renderer.PrintSeparator();

            _renderer.PrintOption(1, "Mine block");
            _renderer.PrintOption(2, "Add transaction to the pool");
            _renderer.PrintOption(3, "Cancel transaction from the pool");
            _renderer.PrintOption(4, "Show MemPool State");
            _renderer.PrintOption(0, "Back");

            _renderer.WriteColored("\n  >>> ", ConsoleColor.Cyan);

            switch (Console.ReadLine())
            {
                case "1":
                    await MineBlock();
                    return;
                case "2":
                    await AddTransaction();
                    break;
                case "3":
                    RemoveTransaction();
                    break;
                case "4":
                    ShowMemPoolState();
                    break;
                case "0":
                    return;
                default:
                    _context.SetStatus("Invalid option.", false);
                    break;
            }
        }
    }
    private void RemoveTransaction()
    {
        try
        {
            Console.Clear();
            _renderer.PrintHeader("REMOVE TRANSACTION");

            _renderer.WriteColored("\n  Sender Address: ", ConsoleColor.Cyan);
            string? senderAddress = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(senderAddress))
            {
                _context.SetStatus("Sender address cannot be null or empty.", false);
                return;
            }

            _renderer.WriteColored("\n  Transaction ID: ", ConsoleColor.Cyan);
            string? transactionId = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(transactionId))
            {
                _context.SetStatus("Transaction's Id cannot be null or empty.", false);
                return;
            }

            var result = _blockchainService.CancelTransaction(transactionId, senderAddress);
            if (!result)
            {
                _context.SetStatus($"Transaction {transactionId} cancellation failed.", result);
                return;
            }
            _context.SetStatus($"Transaction {transactionId} canceled successfully.", result);
            return;
        }
        catch (KeyNotFoundException ex)
        {
            _context.SetStatus(ex.Message, false);
            return;
        }
        catch (Exception ex)
        {
            _context.SetStatus(ex.Message, false);
            return;
        }
    }
    private async Task MineBlock()
    {
        try
        {
            Console.Clear();
            _renderer.PrintHeader("MINE BLOCK");

            _renderer.WriteColored("\n  Miner Address: ", ConsoleColor.Cyan);
            string? minerAddress = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(minerAddress))
            {
                _context.SetStatus("Miner address cannot be null or empty.", false);
                return;
            }

            _renderer.PrintStatus("Mining block...", ConsoleColor.Yellow);

            var (block, metrics) = _blockchainService.MinePendingTransactions(minerAddress);
            _context.LastMiningMetrics = metrics;
            await _networkService.BroadcastAsync("Block", block);


            _context.SetStatus($"Block #{block.Index} mined successfully.");
        }
        catch (Exception ex)
        {
            _context.SetStatus(ex.Message, false);
        }
    }
    private async Task AddTransaction()
    {
        Console.Clear();
        _renderer.PrintHeader("ADD TRANSACTION");

        _renderer.WriteColored(
            "\n  Type (0 - Transfer, 1 - Purchase, 2 - Gift): ",
            ConsoleColor.Cyan);

        TransactionType? type = GetTransactionType(Console.ReadLine());

        _renderer.WriteColored("  From: ", ConsoleColor.Cyan);
        string? from = Console.ReadLine();

        _renderer.WriteColored("  To: ", ConsoleColor.Cyan);
        string? to = Console.ReadLine();

        _renderer.WriteColored("  Amount: ", ConsoleColor.Cyan);
        decimal amount = decimal.TryParse(Console.ReadLine(), out decimal parsed) ? parsed : -1;

        _renderer.WriteColored("  Fee: ", ConsoleColor.Cyan);
        decimal fee = decimal.TryParse(Console.ReadLine(), out decimal parsedFee) ? parsedFee : -1;

        try
        {
            var sender = _walletService.Wallets
                .FirstOrDefault(w => w.Address == from);

            var transaction = _transactionService.CreateTransaction(type, sender, to, amount, fee);
            var validationResult = _transactionService.ValidateTransactionWithBalance(
                transaction,
                _blockchainService.Chain,
                _blockchainService.MemPool);

            if (!validationResult.IsValid)
            {
                _context.SetStatus(validationResult.Message, false);
                return;
            }

            var newTransaction = _blockchainService.AddTransaction(transaction);
            await _networkService.BroadcastAsync(
                type: "Transaction",
                data: newTransaction);

            _context.SetStatus($"Transaction added to pool. Pool: {_blockchainService.MemPool.Count}");
        }
        catch (Exception ex)
        {
            _context.SetStatus(ex.Message, false);
        }
    }
    private TransactionType? GetTransactionType(string? value)
    {
        return value switch
        {
            "0" => TransactionType.Transfer,
            "1" => TransactionType.Purchase,
            "2" => TransactionType.Gift,
            _ => null
        };
    }
    private void ShowChain()
    {
        Console.Clear();
        _displayService.Display(_blockchainService.Chain);
        _renderer.WaitForKey();
    }
    private void Validate()
    {
        Console.Clear();
        _renderer.PrintHeader("VALIDATE CHAIN");

        try
        {
            var validationResult = _blockchainService.Validate(_blockchainService.Chain);
            _context.SetStatus(validationResult.Message, validationResult.IsValid);
        }
        catch (Exception ex)
        {
            _context.SetStatus(ex.Message, false);
        }
    }
    private void Remine()
    {
        Console.Clear();
        _renderer.PrintHeader("RE-MINE BLOCK");

        var block = SelectBlock();

        if (block == null)
        {
            _context.SetStatus("Invalid block selection.", false);
            return;
        }
        try
        {
            _renderer.PrintStatus("Re-mining block...", ConsoleColor.Yellow);

            _context.LastMiningMetrics = _blockchainService.RemineBlock(block);
            _context.LastRepairMetrics = null;

            _context.SetStatus($"Block #{block.Index} re-mined successfully.");
        }
        catch (Exception ex)
        {
            _context.SetStatus(ex.Message, false);
        }
    }
    private void Fix()
    {
        Console.Clear();
        _renderer.PrintHeader("REPAIR CHAIN");

        _renderer.WriteColored("\n  Start index (0 for genesis): ", ConsoleColor.Cyan);

        if (!int.TryParse(Console.ReadLine(), out int startIndex)
            || startIndex < 0
            || startIndex >= _blockchainService.Chain.Count)
        {
            _context.SetStatus("Invalid starting index.", false);
            return;
        }
        try
        {
            _renderer.PrintStatus("Repairing chain...", ConsoleColor.Yellow);

            _context.LastRepairMetrics = _blockchainService.RepairChain(startIndex);
            _context.LastMiningMetrics = null;

            var result = _blockchainService.Validate(_blockchainService.Chain);

            _context.SetStatus(result.IsValid
                    ? "Chain repaired successfully."
                    : "Repair completed, but validation failed.",
                result.IsValid);
        }
        catch (Exception ex)
        {
            _context.SetStatus(ex.Message, false);
        }
    }
    private void ShowChainMetrics()
    {
        Console.Clear();
        _renderer.PrintHeader("CHAIN METRICS");

        try
        {
            var result = _blockchainService.GetChainMetrics();

            _renderer.PrintInfo("Fastest block", result.FastestBlock.Index.ToString());
            _displayService.DisplayBlock(result.FastestBlock);

            _renderer.PrintInfo("Slowest block", result.SlowestBlock.Index.ToString());
            _displayService.DisplayBlock(result.SlowestBlock);

            _renderer.PrintInfo("Most attempts block", result.MostAttemptsBlock.Index.ToString());

            _displayService.DisplayBlock(result.MostAttemptsBlock);

            _renderer.PrintInfo("Average mining time", result.AvgMiningTime.ToString());
            _renderer.PrintInfo("Average attempts count", result.AvgAttemptsCount.ToString());
            _renderer.PrintInfo("Global min difficulty", result.MinDiffAtMining.ToString());
            _renderer.PrintInfo("Global max difficulty", result.MaxDiffAtMining.ToString());

            _renderer.WaitForKey();
        }
        catch (Exception ex)
        {
            _context.SetStatus(ex.Message, false);
        }
    }
    //private void ChangeData()
    //{
    //    Console.Clear();
    //    PrintHeader("CHANGE BLOCK DATA");

    //    var block = SelectBlock(false);

    //    if (block == null)
    //    {
    //        ShowMessage("Invalid block selection.", false);
    //        return;
    //    }

    //    PrintInfo("Current Data", block.Data);
    //    WriteColored("\n  New data: ", ConsoleColor.Cyan);

    //    string? data = Console.ReadLine();

    //    if (string.IsNullOrWhiteSpace(data))
    //    {
    //        ShowMessage("Block data cannot be empty.", false);
    //        return;
    //    }

    //    try
    //    {
    //        PrintStatus("Updating block...", ConsoleColor.Yellow);

    //        _blockchainService.ChangeData(block, data);

    //        ShowMessage($"Block #{block.Index} updated. Chain may require repair.");
    //    }
    //    catch (Exception ex)
    //    {
    //        ShowMessage(ex.Message, false);
    //    }
    //}
    private Block? SelectBlock(bool allowGenesis = true)
    {
        int minIndex = allowGenesis ? 0 : 1;

        Console.WriteLine();

        _renderer.PrintInfo("Available", $"{minIndex} - {_blockchainService.Chain.Count - 1}");

        _renderer.WriteColored("\n  Block ID: ", ConsoleColor.Cyan);

        if (!int.TryParse(Console.ReadLine(), out int id) || id < minIndex)
            return null;

        return _blockchainService.Chain.Find(block => block.Index == id);
    }
    private void ShowMenu()
    {
        _renderer.PrintHeader("BLOCKCHAIN");
        _renderer.ShowStatusBar();

        Console.WriteLine();

        _renderer.PrintInfo("Difficulty", _blockchainService.Difficulty.ToString());
        _renderer.PrintInfo("Blocks", _blockchainService.Chain.Count.ToString());
        _renderer.PrintInfo("Pool", _blockchainService.MemPool.Count.ToString());

        _renderer.PrintSeparator();

        _renderer.PrintOption(1, "Change Difficulty");
        _renderer.PrintOption(2, "Add Block");
        _renderer.PrintOption(3, "Show Chain");
        _renderer.PrintOption(4, "Validate");
        _renderer.PrintOption(5, "Re-mine Block");
        _renderer.PrintOption(6, "Repair Chain");
        _renderer.PrintOption(7, "Toggle Metrics");
        _renderer.PrintOption(8, "Show Chain Metrics");
        _renderer.PrintOption(9, "Change Transaction Amount");
        _renderer.PrintOption(10, "Validate Economy");
        _renderer.PrintOption(11, "Validate Mining Rewards");
        _renderer.PrintOption(12, "Display Merkle Tree");
        _renderer.PrintOption(13, "Merkle Proof");
        _renderer.PrintOption(14, "Save");
        _renderer.PrintOption(15, "Sync");
        _renderer.PrintOption(16, "Get Node Stats");
        _renderer.PrintOption(0, "Back");

        _renderer.WriteColored("\n  >>> ", ConsoleColor.Cyan);
    }
}