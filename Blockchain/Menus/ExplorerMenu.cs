using Blockchain.Menus.Interfaces;
using Blockchain.Services;
using Blockchain.UI;

namespace Blockchain.Menus;

public class ExplorerMenu: IMenu
{
    private readonly ExplorerService _explorerService;
    private readonly DisplayService _displayService;
    private readonly ConsoleRenderer _renderer;
    private readonly MenuContext _menuContext;

    public ExplorerMenu(
        ExplorerService explorerService,
        DisplayService displayService,
        ConsoleRenderer consoleRenderer,
        MenuContext menuContext)
    {
        _explorerService = explorerService;
        _displayService = displayService;
        _renderer = consoleRenderer;
        _menuContext = menuContext;
    }

    public void Run()
    {
        while (true)
        {
            _renderer.PrintHeader("CRYPTO EXPLORER");
            _renderer.ShowStatusBar();

            Console.WriteLine();

            _renderer.PrintOption(1, "Find by ID");
            _renderer.PrintOption(2, "Find by Sender");
            _renderer.PrintOption(3, "Find by Amount+");
            _renderer.PrintOption(4, "Biggest Transaction");
            _renderer.PrintOption(5, "Find by Type");
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
                case "0":
                    return;
                default:
                    _menuContext.SetStatus("Invalid option.", false);
                    break;
            }
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