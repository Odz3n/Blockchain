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
    private readonly ConsoleRenderer _renderer;
    private readonly MenuContext _context;

    public BlockchainMenu(
        BlockchainService blockchainService,
        TransactionService transactionService,
        WalletService walletService,
        DisplayService displayService,
        ConsoleRenderer renderer,
        MenuContext context)
    {
        _blockchainService = blockchainService;
        _transactionService = transactionService;
        _walletService = walletService;
        _displayService = displayService;
        _renderer = renderer;
        _context = context;
    }

    public void Run()
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
                    AddBlock();
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
                case "0":
                    return;
                default:
                    _context.SetStatus("Invalid menu option.", false);
                    break;
            }
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
    private void AddBlock()
    {
        while (true)
        {
            Console.Clear();
            _renderer.PrintHeader("ADD BLOCK");
            _renderer.ShowStatusBar();

            Console.WriteLine();

            _renderer.PrintInfo(
                "Transactions in pool",
                _blockchainService.TransactionPool.Count.ToString());

            _renderer.PrintSeparator();

            _renderer.PrintOption(1, "Mine block");
            _renderer.PrintOption(2, "Add transaction to pool");
            _renderer.PrintOption(0, "Back");

            _renderer.WriteColored("\n  >>> ", ConsoleColor.Cyan);

            switch (Console.ReadLine())
            {
                case "1":
                    MineBlock();
                    return;
                case "2":
                    AddTransaction();
                    break;
                case "0":
                    return;
                default:
                    _context.SetStatus("Invalid option.", false);
                    break;
            }
        }
    }
    private void MineBlock()
    {
        if (_blockchainService.TransactionPool.Count == 0)
        {
            _context.SetStatus("Transaction pool is empty.", false);
            return;
        }
        try
        {
            _renderer.PrintStatus("Mining block...", ConsoleColor.Yellow);

            var metrics = _blockchainService.AddBlock(
                _blockchainService.TransactionPool,
                "N/A");

            if (metrics != null)
                _context.ChangeMetrics = metrics;

            var block = _blockchainService.Chain[^1];

            _context.SetStatus($"Block #{block.Index} mined successfully.");
        }
        catch (Exception ex)
        {
            _context.SetStatus(ex.Message, false);
        }
    }
    private void AddTransaction()
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

        try
        {
            var sender = _walletService.Wallets
                .FirstOrDefault(w => w.Address == from);

            var transaction = _transactionService.CreateTransaction(type, sender, to, amount);
            var validationResult = _transactionService.ValidateTransaction(transaction);

            if (!validationResult.IsValid)
            {
                _context.SetStatus(validationResult.Message, false);
                return;
            }

            _blockchainService.AddTransaction(transaction);

            _context.SetStatus($"Transaction added to pool. Pool: {_blockchainService.TransactionPool.Count}");
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
            var validationResult = _blockchainService.IsValid();
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

            var result = _blockchainService.IsValid();

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
        _renderer.PrintInfo("Pool", _blockchainService.TransactionPool.Count.ToString());

        _renderer.PrintSeparator();

        _renderer.PrintOption(1, "Change Difficulty");
        _renderer.PrintOption(2, "Add Block");
        _renderer.PrintOption(3, "Show Chain");
        _renderer.PrintOption(4, "Validate");
        _renderer.PrintOption(5, "Re-mine Block");
        _renderer.PrintOption(6, "Repair Chain");
        _renderer.PrintOption(7, "Toggle Metrics");
        _renderer.PrintOption(8, "Show Chain Metrics");
        _renderer.PrintOption(0, "Back");

        _renderer.WriteColored("\n  >>> ", ConsoleColor.Cyan);
    }
}