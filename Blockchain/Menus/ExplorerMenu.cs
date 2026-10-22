using Blockchain.Menus.Interfaces;
using Blockchain.Services;
using Blockchain.UI;

namespace Blockchain.Menus;

public class ExplorerMenu: IMenu
{
    private readonly WalletService _walletService;
    private readonly ExplorerService _explorerService;
    private readonly DisplayService _displayService;
    private readonly ConsoleRenderer _renderer;
    private readonly MenuContext _menuContext;

    public ExplorerMenu(
        WalletService walletService,
        ExplorerService explorerService,
        DisplayService displayService,
        ConsoleRenderer consoleRenderer,
        MenuContext menuContext)
    {
        _walletService = walletService;
        _explorerService = explorerService;
        _displayService = displayService;
        _renderer = consoleRenderer;
        _menuContext = menuContext;
    }

    public async Task Run()
    {
        while (true)
        {
            Console.Clear();

            _renderer.PrintHeader("CRYPTO EXPLORER");
            _renderer.ShowStatusBar();

            Console.WriteLine();

            _renderer.PrintOption(1, "Find by ID");
            _renderer.PrintOption(2, "Find by Sender");
            _renderer.PrintOption(3, "Find by Amount+");
            _renderer.PrintOption(4, "Biggest Transaction");
            _renderer.PrintOption(5, "Find by Type");
            _renderer.PrintOption(6, "Transaction History");
            _renderer.PrintOption(7, "Wallet Statement");
            _renderer.PrintOption(8, "Total Coins Created");
            _renderer.PrintOption(0, "Back");

            _renderer.WriteColored("\n  >>> ", ConsoleColor.Cyan);

            switch (Console.ReadLine())
            {
                case "1":
                    GetTransactionById();
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
                    GetTransactionByType();
                    break;
                case "6":
                    GetTransactionHistory();
                    break;
                case "7":
                    GetWalletStatement();
                    break;
                case "8":
                    GetTotalCoinsCreated();
                    break;
                case "0":
                    return;
                default:
                    _menuContext.SetStatus("Invalid option.", false);
                    break;
            }
        }
    }

    private void GetTotalCoinsCreated()
    {
        try
        {
            _menuContext.SetStatus($"{_explorerService.GetTotalCoinsCreated()} total coins created.");
            return;
        }
        catch (KeyNotFoundException ex)
        {
            _menuContext.SetStatus(ex.Message, false);
            return;
        }
        catch (Exception ex)
        {
            _menuContext.SetStatus(ex.Message, false);
            return;
        }
    }

    private void GetWalletStatement()
    {
        Console.Clear();
        _renderer.PrintHeader("WALLET STATEMENT");

        _renderer.WriteColored("\n  Wallet owner address: ", ConsoleColor.Cyan);
        string? walletOwnerAddress = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(walletOwnerAddress))
        {
            _menuContext.SetStatus("Wallet owner address cannot be empty.", false);
            return;
        }

        try
        {
            var wallet = _walletService.GetWallets()
                .FirstOrDefault(w => w.Address == walletOwnerAddress);
            if (wallet == null)
            {
                _menuContext.SetStatus($"Wallet for address '{walletOwnerAddress}' not found.", false);
                return;
            }

            _displayService.DisplayWalletStatement(wallet);
            _renderer.WaitForKey();
        }
        catch (Exception ex)
        {
            _menuContext.SetStatus(ex.Message, false);
            return;
        }
    }

    private void GetTransactionHistory()
    {
        Console.Clear();
        _renderer.PrintHeader("TRANSACTION HISTORY");

        _renderer.WriteColored("\n  Wallet owner address: ", ConsoleColor.Cyan);
        string? walletOwnerAddress = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(walletOwnerAddress))
        {
            _menuContext.SetStatus("Wallet owner address cannot be empty.", false);
            return;
        }

        try
        {
            var wallet = _walletService.GetWallets()
                .FirstOrDefault(w => w.Address == walletOwnerAddress);
            if (wallet == null)
            {
                _menuContext.SetStatus($"Wallet for address '{walletOwnerAddress}' not found.", false);
                return;
            }

            var transactions = _explorerService.GetTransactionHistory(wallet) ?? new();
            _displayService.DisplayTransactionHistory(transactions);
            _renderer.WaitForKey();
        }
        catch (Exception ex)
        {
            _menuContext.SetStatus(ex.Message, false);
            return;
        }
    }
    private void GetTransactionByType()
    {
        Console.Clear();
        _renderer.PrintHeader("TRANSACTIONS BY TYPE");

        _renderer.WriteColored("\n  Type (0 - Transfer, 1 - Purchase, 2 - Gift): ", ConsoleColor.Cyan);
        Models.Type? type = GetTransactionType(Console.ReadLine());

        var transactions = _explorerService.GetByType(type);

        if (transactions == null || transactions.Count <= 0)
        {
            _menuContext.SetStatus($"No {type.ToString()} transactions found.", false);
            return;
        }

        _displayService.DisplayTransactions(transactions);
        _renderer.WaitForKey();
    }
    private Models.Type? GetTransactionType(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            _menuContext.SetStatus("Invalid value.", false);
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
    private void GetTheBiggest()
    {
        Console.Clear();
        _renderer.PrintHeader("BIGGEST TRANSACTION");

        var transaction = _explorerService.GetTheBiggestTransaction();

        if (transaction == null)
        {
            _menuContext.SetStatus("No transactions found.", false);
            return;
        }

        _displayService.DisplayTransaction(transaction);
        _renderer.WaitForKey();
    }
    private void GetTransactionsByAmount()
    {
        Console.Clear();
        _renderer.PrintHeader("TRANSACTIONS BY AMOUNT");

        _renderer.WriteColored("\n  Amount: ", ConsoleColor.Cyan);

        if (!decimal.TryParse(Console.ReadLine(), out decimal amount) || amount < 0)
        {
            _menuContext.SetStatus("Invalid amount.", false);
            return;
        }

        var transactions = _explorerService.GetTransactionsWithAmountGreaterThan(amount);

        if (transactions.Count == 0)
        {
            _menuContext.SetStatus($"No transactions greater than {amount:F2}.", false);
            return;
        }

        foreach (var transaction in transactions)
            _displayService.DisplayTransaction(transaction);

        _renderer.WaitForKey();
    }
    private void GetTransactionsByUser()
    {
        Console.Clear();
        _renderer.PrintHeader("TRANSACTIONS BY SENDER");

        _renderer.WriteColored("\n  Sender: ", ConsoleColor.Cyan);
        string? sender = Console.ReadLine();

        if (string.IsNullOrWhiteSpace(sender))
        {
            _menuContext.SetStatus("Sender cannot be empty.", false);
            return;
        }

        var transactions = _explorerService.GetTransactionsBySender(sender);

        if (transactions.Count == 0)
        {
            _menuContext.SetStatus($"No transactions found for '{sender}'.", false);
            return;
        }

        foreach (var transaction in transactions)
            _displayService.DisplayTransaction(transaction);

        _renderer.WaitForKey();
    }
    private void GetTransactionById()
    {
        Console.Clear();
        _renderer.PrintHeader("FIND TRANSACTION");

        _renderer.WriteColored("\n  Transaction ID: ", ConsoleColor.Cyan);
        string? id = Console.ReadLine();

        if (string.IsNullOrWhiteSpace(id))
        {
            _menuContext.SetStatus("Transaction ID cannot be empty.", false);
            return;
        }

        var transaction = _explorerService.FindById(id);

        if (transaction == null)
        {
            _menuContext.SetStatus($"Transaction '{id}' not found.", false);
            return;
        }

        _displayService.DisplayTransaction(transaction);
        _renderer.WaitForKey();
    }
}