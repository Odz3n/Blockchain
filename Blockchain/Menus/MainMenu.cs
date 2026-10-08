using Blockchain.Menus.Interfaces;
using Blockchain.Services;
using Blockchain.UI;

namespace Blockchain.Menus;

public class MainMenu: IMenu
{
    private readonly BlockchainService _blockchainService;
    private readonly DisplayService _displayService;
    private readonly ConsoleRenderer _renderer;
    private readonly MenuContext _context;

    private readonly IMenu _blockchainMenu;
    private readonly IMenu _explorerMenu;
    private readonly IMenu _walletMenu;

    public MainMenu(
        BlockchainService blockchainService,
        DisplayService displayService,
        ConsoleRenderer renderer,
        MenuContext context,
        BlockchainMenu blockchainMenu,
        ExplorerMenu explorerMenu,
        WalletMenu walletMenu)
    {
        _blockchainService = blockchainService;
        _displayService = displayService;
        _renderer = renderer;
        _context = context;

        _blockchainMenu = blockchainMenu;
        _explorerMenu = explorerMenu;
        _walletMenu = walletMenu;
    }

    public void Run()
    {
        while (true)
        {
            Console.Clear();
            ShowMenu();

            if (!int.TryParse(Console.ReadLine(), out int actionId))
            {
                _context.SetStatus("Invalid menu option.", false);
                continue;
            }

            if (actionId == 0)
                return;

            switch (actionId)
            {
                case 1:
                    _blockchainMenu.Run();
                    break;
                case 2:
                    _explorerMenu.Run();
                    break;
                case 3:
                    _walletMenu.Run();
                    break;
                default:
                    _context.SetStatus("Unknown menu option.", false);
                    break;
            }
        }
    }
    private void ShowMenu()
    {
        _renderer.PrintHeader("BLOCKCHAIN MANAGER");
        _renderer.ShowStatusBar();

        Console.WriteLine();

        _renderer.PrintInfo("Difficulty", _blockchainService.Difficulty.ToString());
        _renderer.PrintInfo("Blocks", _blockchainService.Chain.Count.ToString());
        _renderer.PrintInfo("Metrics", _context.ShowMetrics ? "ON" : "OFF");

        if (_context.ShowMetrics)
        {
            if (_context.LastRepairMetrics != null)
                _displayService.DisplayRepairMetrics(_context.LastRepairMetrics);

            if (_context.LastMiningMetrics != null)
                _displayService.DisplayMiningMetrics(_context.LastMiningMetrics);

            if (_context.ChangeMetrics != null)
                _displayService.DisplayDifficultyChangeMetrics(_context.ChangeMetrics);
        }

        _renderer.PrintSeparator();

        _renderer.PrintOption(1, "Blockchain");
        _renderer.PrintOption(2, "Crypto Explorer");
        _renderer.PrintOption(3, "Wallets");
        _renderer.PrintOption(0, "Exit");

        _renderer.WriteColored("\n  >>> ", ConsoleColor.Cyan);
    }
}