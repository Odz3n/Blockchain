using Blockchain.Menus.Interfaces;
using Blockchain.Services;
using Blockchain.UI;
using System.Security.Cryptography;
using System.Text;

namespace Blockchain.Menus;

public class WalletMenu : IMenu
{
    private readonly BlockchainService _blockchainService;
    private readonly WalletService _walletService;
    private readonly DisplayService _displayService;
    private readonly ConsoleRenderer _renderer;
    private readonly MenuContext _context;

    public WalletMenu(
        BlockchainService blockchainService,
        WalletService walletService,
        DisplayService displayService,
        ConsoleRenderer renderer,
        MenuContext context)
    {
        _blockchainService = blockchainService;
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
            _renderer.PrintOption(3, "Verify transaction signature");
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
                case "3":
                    VerifyTransactionSignature();
                    break;
                case "0":
                    return;
                default:
                    _context.SetStatus("Invalid option.", false);
                    break;
            }
        }
    }

    private void VerifyTransactionSignature()
    {
        Console.Clear();
        _renderer.PrintHeader("VERIFY TRANSACTION SIGNATURE");

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

        _renderer.WriteColored("\n  Public key (Enter to use transaction key): ", ConsoleColor.Cyan);
        string? publicKeyInput = Console.ReadLine();

        try
        {
            byte[] publicKey = string.IsNullOrWhiteSpace(publicKeyInput)
                ? transaction.PublicKey
                : Convert.FromBase64String(publicKeyInput.Trim());

            var result = _walletService.VerifyTransactionSignature(transaction, publicKey);

            string message = result
                ? "Transaction signature verified successfully."
                : "Transaction signature verification failed.";

            ConsoleColor color = result
                ? ConsoleColor.Green
                : ConsoleColor.Red;

            _context.SetStatus(message, result);
            _renderer.PrintStatus(message, color);
            _renderer.WaitForKey();
        }
        catch (FormatException)
        {
            _context.SetStatus("Invalid public key format. Expected Base64.", false);
        }
        catch (CryptographicException)
        {
            _context.SetStatus("Invalid public key or cryptographic parameters.", false);
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