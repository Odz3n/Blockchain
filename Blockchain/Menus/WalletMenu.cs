using Blockchain.Menus.Interfaces;
using Blockchain.Services;
using Blockchain.UI;

namespace Blockchain.Menus;

public class WalletMenu : IMenu
{
    private readonly WalletService _walletService;
    private readonly DisplayService _displayService;
    private readonly ConsoleRenderer _renderer;
    private readonly MenuContext _context;

    public WalletMenu(
        WalletService walletService,
        DisplayService displayService,
        ConsoleRenderer renderer,
        MenuContext context)
    {
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

            _renderer.PrintHeader("WALLETS");
            _renderer.ShowStatusBar();

            Console.WriteLine();

            _renderer.PrintOption(1, "Create Wallet");
            _renderer.PrintOption(2, "List Wallets");
            _renderer.PrintOption(0, "Back");

            _renderer.WriteColored("\n  >>> ", ConsoleColor.Cyan);

            switch (Console.ReadLine())
            {
                case "1":
                    CreateWallet();
                    break;
                case "2":
                    ListWallets();
                    break;
                case "0":
                    return;
                default:
                    _context.SetStatus("Invalid option.", false);
                    break;
            }
        }
    }
    private void CreateWallet()
    {
        Console.Clear();
        _renderer.PrintHeader("CREATE WALLET");

        _renderer.WriteColored("\n  Name: ", ConsoleColor.Cyan);
        string? name = Console.ReadLine();

        if (string.IsNullOrWhiteSpace(name))
        {
            _context.SetStatus("Wallet's name cannot be null or empty.", false);
            return;
        }
        try
        {
            var wallet = _walletService.CreateWallet(name);
            _context.SetStatus($"Wallet for {wallet.Name} created successfully.");
        }
        catch (Exception ex)
        {
            _context.SetStatus(ex.Message, false);
        }
    }

    private void ListWallets()
    {
        Console.Clear();
        _renderer.PrintHeader("WALLETS LIST");

        var wallets = _walletService.GetWallets();

        if (wallets.Count == 0)
        {
            _context.SetStatus("No registered wallets.", false);
            return;
        }

        _displayService.DisplayWallets(wallets);
        _renderer.WaitForKey();
    }
}