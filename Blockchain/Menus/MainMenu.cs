using Blockchain.Menus.Interfaces;
using Blockchain.Models;
using Blockchain.Services;
using Blockchain.UI;
using System.Text.Json;

namespace Blockchain.Menus;

public class MainMenu: IMenu
{
    private readonly BlockchainService _blockchainService;
    private readonly DisplayService _displayService;
    private readonly ConsoleRenderer _renderer;
    private readonly MenuContext _context;
    private readonly NetworkService _networkService;
    private readonly WalletService _walletService;

    private readonly IMenu _blockchainMenu;
    private readonly IMenu _explorerMenu;
    private readonly IMenu _walletMenu;

    public MainMenu(
        BlockchainService blockchainService,
        DisplayService displayService,
        NetworkService networkService,
        WalletService walletService,
        ConsoleRenderer renderer,
        MenuContext context,
        BlockchainMenu blockchainMenu,
        ExplorerMenu explorerMenu,
        WalletMenu walletMenu)
    {
        _blockchainService = blockchainService;
        _displayService = displayService;
        _walletService = walletService;
        _renderer = renderer;
        _context = context;

        _blockchainMenu = blockchainMenu;
        _explorerMenu = explorerMenu;
        _walletMenu = walletMenu;

        _networkService = networkService;
        _networkService.Start((type, message) =>
        {
            _context.SetStatus($"Received message of type {type}.");
            switch (type)
            {
                case "Transaction":
                    {
                        var transaction = JsonSerializer.Deserialize<Transaction>(message);
                        if (transaction != null)
                        {
                            _context.SetStatus($"Received transaction from {transaction.From} to {transaction.To} for amount {transaction.Amount}.");
                            var addingResult = _blockchainService.AddReceivedTransaction(transaction);
                            _context.SetStatus(addingResult.Message, addingResult.IsValid);
                        }
                        break;
                    }
                case "Block":
                    {
                        var block = JsonSerializer.Deserialize<Block>(message);
                        if (block != null)
                        {
                            _context.SetStatus($"Received block with index {block.Index} and hash {block.Hash}");
                            var addingResult = _blockchainService.AddReceivedBlock(block);
                            _context.SetStatus(addingResult.Message, addingResult.IsValid);
                        }
                        break;
                    }
                default:
                    _context.SetStatus($"Received data of unsupported type.");
                    break;
            }
        });
    }

    public async Task Run()
    {
        var blockchainState = await _blockchainService.LoadAsync();
        var restorationResult = _blockchainService.RestoreState(blockchainState);
        if (!restorationResult.IsValid)
        {
            _context.SetStatus(restorationResult.Message, restorationResult.IsValid);
            return;
        }

        var walletsState = await _walletService.LoadAsync();
        restorationResult = _walletService.RestoreState(walletsState);
        if (!restorationResult.IsValid)
        {
            _context.SetStatus(restorationResult.Message, restorationResult.IsValid);
            return;
        }

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
            {
                await _blockchainService.SaveAsync();
                await _walletService.SaveAsync();
                return;
            }

            switch (actionId)
            {
                case 1:
                    await _blockchainMenu.Run();
                    break;
                case 2:
                    await _explorerMenu.Run();
                    break;
                case 3:
                    await _walletMenu.Run();
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
        _renderer.PrintInfo("Port", _networkService.Port.ToString());

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