using Blockchain.Models;
using Blockchain.Models.Metrics;

namespace Blockchain.Services;

public class MenuService
{
    private readonly BlockchainService _blockchainService;
    private readonly DisplayService _displayService;
    private readonly ExplorerService _explorerService;
    private readonly TransactionService _transactionService;
    private readonly WalletService _walletService;

    private string? _statusMessage;
    private bool _statusSuccess = true;
    private MiningMetrics? _lastMiningMetrics;
    private ChainRepairMetrics? _lastRepairMetrics;
    private DifficultyChangeMetrics? _changeMetrics;

    public bool ShowMetrics { get; private set; }

    public MenuService(int initialDifficulty = 2, bool showMetrics = false)
    {
        ShowMetrics = showMetrics;
        _blockchainService = new BlockchainService(initialDifficulty);
        _displayService = new DisplayService();
        _explorerService = new(_blockchainService);
        _transactionService = new();
        _walletService = new();
    }
    public void Run()
    {
        while (true)
        {
            Console.Clear();
            ShowMenuBar();

            if (!int.TryParse(Console.ReadLine(), out int actionId))
            {
                ShowMessage("Invalid menu option.", false);
                continue;
            }

            if (actionId == 0)
                return;

            Action(actionId);
        }
    }
    private void ShowMenuBar()
    {
        PrintHeader("BLOCKCHAIN MANAGER");
        ShowStatusBar();

        Console.WriteLine();

        PrintInfo("Difficulty", _blockchainService.Difficulty.ToString());
        PrintInfo("Blocks", _blockchainService.Chain.Count.ToString());
        PrintInfo("Metrics", ShowMetrics ? "ON" : "OFF");

        if (ShowMetrics)
        {
            if (_lastRepairMetrics != null)
                _displayService.DisplayRepairMetrics(_lastRepairMetrics);
            if (_lastMiningMetrics != null)
                _displayService.DisplayMiningMetrics(_lastMiningMetrics);
            if (_changeMetrics != null)
                _displayService.DisplayDifficultyChangeMetrics(_changeMetrics);
        }

        PrintSeparator();

        PrintOption(101, "Create wallet");
        PrintOption(102, "List wallets");
        PrintOption(1, "Change Difficulty");
        PrintOption(2, "Add Block");
        PrintOption(3, "Show Chain");
        PrintOption(4, "Change Data");
        PrintOption(5, "Validate");
        PrintOption(6, "Re-mine Block");
        PrintOption(7, "Repair Chain");
        PrintOption(8, "Toggle Metrics");
        PrintOption(9, "Show Chain metrics");
        PrintOption(10, "Crypto Explorer");
        PrintOption(0, "Exit");

        Console.Write("\n  >>> ");
    }
    private void Action(int actionId)
    {
        switch (actionId)
        {
            case 1:
                ChangeDifficulty();
                break;
            case 2:
                AddBlock();
                break;
            case 3:
                ShowChain();
                break;
            //case 4:
            //    ChangeData();
            //    break;
            case 5:
                Validate();
                break;
            case 6:
                Remine();
                break;
            case 7:
                Fix();
                break;
            case 8:
                ToggleMetrics();
                break;
            case 9:
                ShowChainMetrics();
                break;
            case 10:
                CryptoExplorer();
                break;
            case 101:
                CreateWallet();
                break;  
            case 102:
                ListWallets();
                break;  
            default:
                ShowMessage("Unknown menu option.", false);
                break;
        }
    }

    private void ListWallets()
    {
        Console.Clear();
        PrintHeader("WALLETS LIST");

        var wallets = _walletService.GetWallets();

        if (wallets.Count == 0)
        {
            ShowMessage($"No registered wallets.", false);
            return;
        }

        _displayService.DisplayWallets(wallets);

        WaitForKey();
    }

    private void CreateWallet()
    {
        Console.Clear();
        PrintHeader("CREATE WALLET");

        WriteColored("\n  Name: ", ConsoleColor.Cyan);
        string? name = Console.ReadLine();

        try
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                ShowMessage("Wallet's name cannot be null or empty.", false);
                return;
            }

            var wallet = _walletService.CreateWallet(name);

            ShowMessage($"Wallet for {wallet.Name} created successfully.", true);
        }
        catch (Exception ex)
        {
            ShowMessage(ex.Message, false);
        }
    }

    private void CryptoExplorer()
    {
        while (true)
        {
            Console.Clear();
            PrintHeader("CRYPTO EXPLORER");

            ShowStatusBar();

            Console.WriteLine();

            PrintOption(1, "Find by Id");
            PrintOption(2, "Find transactions by user (sender)");
            PrintOption(3, "Find amount-specific transactions");
            PrintOption(4, "Find the biggest transaction");
            PrintOption(5, "Find by type");
            PrintOption(0, "Back");

            WriteColored("\n  >>> ", ConsoleColor.Cyan);

            switch (Console.ReadLine())
            {
                case "1":
                    FindTransactionById();
                    break;

                case "2":
                    GetTransactionsByUser();
                    break;

                case "3":
                    GetTransactionsByAmount();
                    break;

                case "4":
                    GetTheBiggest();
                    break;

                case "5":
                    FindTransactionByType();
                    break;

                case "0":
                    return;

                default:
                    ShowMessage("Invalid option.", false);
                    break;
            }
        }
    }
    private void FindTransactionByType()
    {
        Console.Clear();
        PrintHeader("TRANSACTIONS BY TYPE");

        WriteColored("\n  Type (0 - Transfer, 1 - Purchase, 2 - Gift): ", ConsoleColor.Cyan);
        Models.Type? type = GetTransactionType(Console.ReadLine());

        var transactions = _explorerService.GetByType(type);

        if (transactions == null || transactions.Count <= 0)
        {
            ShowMessage($"No {type.ToString()} transactions found.", false);
            return;
        }

        _displayService.DisplayTransactions(transactions);
        WaitForKey();
    }
    private void GetTheBiggest()
    {
        Console.Clear();
        PrintHeader("BIGGEST TRANSACTION");

        var transaction = _explorerService.GetTheBiggestTransaction();

        if (transaction == null)
        {
            ShowMessage("No transactions found.", false);
            return;
        }

        _displayService.DisplayTransaction(transaction);
        WaitForKey();
    }
    private void GetTransactionsByAmount()
    {
        Console.Clear();
        PrintHeader("TRANSACTIONS BY AMOUNT");

        WriteColored("\n  Amount: ", ConsoleColor.Cyan);

        if (!decimal.TryParse(Console.ReadLine(), out decimal amount) || amount < 0)
        {
            ShowMessage("Invalid amount.", false);
            return;
        }

        var transactions = _explorerService.GetTransactionsWithAmountGreaterThan(amount);

        if (transactions.Count == 0)
        {
            ShowMessage($"No transactions greater than {amount:F2}.", false);
            return;
        }

        foreach (var transaction in transactions)
            _displayService.DisplayTransaction(transaction);

        WaitForKey();
    }
    private void GetTransactionsByUser()
    {
        Console.Clear();
        PrintHeader("TRANSACTIONS BY SENDER");

        WriteColored("\n  Sender: ", ConsoleColor.Cyan);
        string? sender = Console.ReadLine();

        if (string.IsNullOrWhiteSpace(sender))
        {
            ShowMessage("Sender cannot be empty.", false);
            return;
        }

        var transactions = _explorerService.GetTransactionsBySender(sender);

        if (transactions.Count == 0)
        {
            ShowMessage($"No transactions found for '{sender}'.", false);
            return;
        }

        foreach (var transaction in transactions)
            _displayService.DisplayTransaction(transaction);

        WaitForKey();
    }
    private void FindTransactionById()
    {
        Console.Clear();
        PrintHeader("FIND TRANSACTION");

        WriteColored("\n  Transaction ID: ", ConsoleColor.Cyan);
        string? id = Console.ReadLine();

        if (string.IsNullOrWhiteSpace(id))
        {
            ShowMessage("Transaction ID cannot be empty.", false);
            return;
        }

        var transaction = _explorerService.FindById(id);

        if (transaction == null)
        {
            ShowMessage($"Transaction '{id}' not found.", false);
            return;
        }

        _displayService.DisplayTransaction(transaction);
        WaitForKey();
    }
    private void ShowChainMetrics()
    {
        Console.Clear();
        PrintHeader("CHAIN METRICS");

        var res = _blockchainService.GetChainMetrics();

        PrintInfo("Fastest block", res.FastestBlock.Index.ToString());
        _displayService.DisplayBlock(res.FastestBlock);

        PrintInfo("Slowest block", res.SlowestBlock.Index.ToString());
        _displayService.DisplayBlock(res.SlowestBlock);

        PrintInfo("Most attempts block", res.MostAttemptsBlock.Index.ToString());
        _displayService.DisplayBlock(res.MostAttemptsBlock);

        PrintInfo("Average mining time", res.AvgMiningTime.ToString());
        PrintInfo("Average attempts count", res.AvgAttemptsCount.ToString());
        PrintInfo("Global min difficulty", res.MinDiffAtMining.ToString());
        PrintInfo("Global max difficulty", res.MaxDiffAtMining.ToString());

        WaitForKey();
    }
    private void ChangeDifficulty()
    {
        Console.Clear();
        PrintHeader("CHANGE DIFFICULTY");
        PrintInfo("Current", _blockchainService.Difficulty.ToString());

        WriteColored("\n  New difficulty: ", ConsoleColor.Cyan);

        if (!int.TryParse(Console.ReadLine(), out int difficulty))
        {
            ShowMessage("Invalid difficulty.", false);
            return;
        }

        try
        {
            _blockchainService.ChangeDifficulty(difficulty);
            ShowMessage($"Difficulty changed to {difficulty}.");
        }
        catch (ArgumentException ex)
        {
            ShowMessage(ex.Message, false);
        }
    }
    private void AddBlock()
    {
        while (true)
        {
            Console.Clear();
            PrintHeader("ADD BLOCK");

            ShowStatusBar();

            Console.WriteLine();

            PrintInfo("Transactions in pool", _blockchainService.TransactionPool.Count.ToString());

            PrintSeparator();

            PrintOption(1, "Add block");
            PrintOption(2, "Add transaction to pool");
            PrintOption(3, "Back");

            WriteColored("\n  >>> ", ConsoleColor.Cyan);

            switch (Console.ReadLine())
            {
                case "1":
                    MineBlock();
                    return;

                case "2":
                    AddTransaction();
                    break;

                case "3":
                    return;

                default:
                    ShowMessage("Invalid option.", false);
                    return;
            }
        }
    }
    private void MineBlock()
    {
        if (_blockchainService.TransactionPool.Count == 0)
        {
            ShowMessage("Transaction pool is empty.", false);
            return;
        }

        try
        {
            PrintStatus("Mining block...", ConsoleColor.Yellow);

            var metrics = _blockchainService.AddBlock(_blockchainService.TransactionPool, "N/A");

            if (metrics != null)
                _changeMetrics = metrics;

            var block = _blockchainService.Chain[^1];

            ShowMessage($"Block #{block.Index} mined successfully.");
        }
        catch (Exception ex)
        {
            ShowMessage(ex.Message, false);
        }
    }
    private void AddTransaction()
    {
        Console.Clear();
        PrintHeader("ADD TRANSACTION");

        WriteColored("\n  Type (0 - Transfer, 1 - Purchase, 2 - Gift): ", ConsoleColor.Cyan);
        Models.Type? type = GetTransactionType(Console.ReadLine());

        WriteColored("  From: ", ConsoleColor.Cyan);
        string? from = Console.ReadLine();

        WriteColored("  To: ", ConsoleColor.Cyan);
        string? to = Console.ReadLine();

        WriteColored("  Amount: ", ConsoleColor.Cyan);
        decimal amount = decimal.TryParse(Console.ReadLine(), out decimal parsed) ? parsed : -1;

        try
        {
            var sender = _walletService.Wallets.FirstOrDefault(w => w.Address == from);

            var transaction = _transactionService.CreateTransaction(type, sender, to, amount);
            var validationResult = _transactionService.ValidateTransaction(transaction);

            if (!validationResult.IsValid)
            {
                ShowMessage(validationResult.Message, validationResult.IsValid);
                return;
            }

            _blockchainService.AddTransaction(transaction);

            ShowMessage($"Transaction added to pool. Pool: {_blockchainService.TransactionPool.Count}");
        }
        catch (Exception ex)
        {
            ShowMessage(ex.Message, false);
        }
    }

    private Models.Type? GetTransactionType(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            ShowMessage("Invalid value.", false);
            return null;
        }

        return value switch
        {
            "0" => Models.Type.Transfer,
            "1" => Models.Type.Purchase,
            "2" => Models.Type.Gift,
            _ => null
        };
    }

    private void ShowChain()
    {
        Console.Clear();
        _displayService.Display(_blockchainService.Chain);
        WaitForKey();
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
    private void Validate()
    {
        Console.Clear();
        PrintHeader("VALIDATE CHAIN");

        try
        {
            var res = _blockchainService.IsValid();

            ShowMessage(res.Message, res.IsValid);
        }
        catch (Exception ex)
        {
            ShowMessage(ex.Message, false);
        }
    }
    private void Remine()
    {
        Console.Clear();
        PrintHeader("RE-MINE BLOCK");

        var block = SelectBlock();

        if (block == null)
        {
            ShowMessage("Invalid block selection.", false);
            return;
        }

        try
        {
            PrintStatus("Re-mining block...", ConsoleColor.Yellow);

            _lastMiningMetrics = _blockchainService.RemineBlock(block);
            _lastRepairMetrics = null;

            ShowMessage($"Block #{block.Index} re-mined successfully.");
        }
        catch (Exception ex)
        {
            ShowMessage(ex.Message, false);
        }
    }
    private void Fix()
    {
        Console.Clear();
        PrintHeader("REPAIR CHAIN");

        WriteColored("\n  Start index (0 for genesis): ", ConsoleColor.Cyan);

        if (!int.TryParse(Console.ReadLine(), out int startIndex) || startIndex < 0 || startIndex >= _blockchainService.Chain.Count)
        {
            ShowMessage("Invalid starting index.", false);
            return;
        }

        try
        {
            PrintStatus("Repairing chain...", ConsoleColor.Yellow);

            _lastRepairMetrics = _blockchainService.RepairChain(startIndex);
            _lastMiningMetrics = null;

            var res = _blockchainService.IsValid();

            ShowMessage(res.IsValid ? "Chain repaired successfully." : "Repair completed, but validation failed.", res.IsValid);
        }
        catch (Exception ex)
        {
            ShowMessage(ex.Message, false);
        }
    }
    private void ToggleMetrics()
    {
        ShowMetrics = !ShowMetrics;
        ShowMessage($"Mining metrics {(ShowMetrics ? "enabled" : "disabled")}.");
    }
    private Block? SelectBlock(bool allowGenesis = true)
    {
        int minIndex = allowGenesis ? 0 : 1;

        Console.WriteLine();
        PrintInfo("Available", $"{minIndex} - {_blockchainService.Chain.Count - 1}");

        WriteColored("\n  Block ID: ", ConsoleColor.Cyan);

        if (!int.TryParse(Console.ReadLine(), out int id) || id < minIndex)
            return null;

        return _blockchainService.Chain.Find(block => block.Index == id);
    }
    private void ShowMessage(string message, bool success = true)
    {
        _statusMessage = message;
        _statusSuccess = success;
    }
    private void ShowStatusBar()
    {
        if (string.IsNullOrWhiteSpace(_statusMessage))
            return;

        Console.WriteLine();

        WriteColored(
            _statusSuccess ? "  [+] " : "  [!] ",
            _statusSuccess ? ConsoleColor.Green : ConsoleColor.Red);

        Console.WriteLine(_statusMessage);
    }
    private void WaitForKey()
    {
        WriteColored("\n  Press any key to continue...", ConsoleColor.DarkGray);
        Console.ReadKey(true);
    }
    private void PrintHeader(string title)
    {
        WriteColored($"\n  {title}\n", ConsoleColor.Cyan);
        PrintSeparator();
    }
    private void PrintSeparator()
    {
        WriteColored("----------------------------------------------\n", ConsoleColor.DarkGray);
    }
    private void PrintOption(int number, string title)
    {
        WriteColored($"  [{number}] ", ConsoleColor.Cyan);
        Console.WriteLine(title);
    }
    private void PrintInfo(string label, string value)
    {
        WriteColored($"  {label,-14}: ", ConsoleColor.DarkGray);
        Console.WriteLine(value);
    }
    private void PrintStatus(string message, ConsoleColor color)
    {
        WriteColored($"\n  {message}\n", color);
    }
    private void WriteColored(string text, ConsoleColor color)
    {
        var previous = Console.ForegroundColor;
        Console.ForegroundColor = color;
        Console.Write(text);
        Console.ForegroundColor = previous;
    }
}