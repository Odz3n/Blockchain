using Blockchain.Models;
using Blockchain.Models.Validation;
using System.Security.Cryptography;
using System.Text;

namespace Blockchain.Services;

public class WalletService
{
    private readonly StorageService _storageService;
    public WalletService()
    {
        _storageService = new();
    }
    public Wallet CreateWallet(string name)
    {
        using (var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256))
        {
            var privateKey = ecdsa.ExportECPrivateKey();
            var publicKey = ecdsa.ExportSubjectPublicKeyInfo();

            var address = Convert.ToBase64String(SHA256.HashData(publicKey)).Substring(0, 10);
            var wallet = new Wallet(name, address, publicKey, privateKey);

            Wallets.Add(wallet);

            return wallet;
        }
    }
    public bool VerifyTransactionSignature(Transaction transaction, byte[] publicKey)
    {
        using (var ecdsa = ECDsa.Create())
        {
            ecdsa.ImportSubjectPublicKeyInfo(publicKey, out int _);
            return ecdsa.VerifyData(Encoding.UTF8.GetBytes(transaction.ToRawString()), transaction.Signature, HashAlgorithmName.SHA256);
        }
    }
    public string? GetNameByAddress(string address)
    {
        return Wallets
            .Where(w => w.Address == address)
            .Select(w => w.Name)
            .FirstOrDefault()??"N/A"
            .Trim();
    }
    public async Task SaveAsync()
    {
        foreach (var wallet in Wallets)
        {
            await _storageService.SaveAsync(wallet);
        }
    }
    public async Task<List<WalletState>> LoadAsync()
    {
        var walletStates = new List<WalletState>();

        foreach (var userDirectory in Directory.EnumerateDirectories(Path.Combine(Directory.GetCurrentDirectory(), "Wallets")))
            walletStates.Add(await _storageService.LoadAsync<WalletState>(Path.Combine(userDirectory, "wallet.json")));

        return walletStates;
    }
    public ValidationResult RestoreState(List<WalletState> walletsState)
    {
        foreach (var state in walletsState)
        {
            Wallets.Add(new Wallet(state.Name, state.Address, state.PublicKey, null));
        }
        return new ValidationResult
        {
            IsValid = true,
            Message = "Wallets restored successfully. PrivateKey import required. Check Wallets menu section."
        };
    }
    public ValidationResult ImportPrivateKey(
        Wallet wallet,
        byte[] privateKey)
    {
        try
        {
            using var ecdsa = ECDsa.Create();
            
            ecdsa.ImportECPrivateKey(privateKey, out _);
            byte[] derivedPublicKey = ecdsa.ExportSubjectPublicKeyInfo();

            var isValidPrivateKey = CryptographicOperations.FixedTimeEquals(derivedPublicKey, wallet.PublicKey);
            if (isValidPrivateKey)
            {
                wallet.PrivateKey = privateKey.ToArray();
                return new ValidationResult
                {
                    IsValid = isValidPrivateKey,
                    Message = "Private Key is Valid."
                };
            }

            return new ValidationResult
            {
                IsValid = isValidPrivateKey,
                Message = "Private Key is Not Valid."
            };
        }
        catch (CryptographicException ex)
        {
            return new ValidationResult
            {
                IsValid = false,
                Message = ex.Message
            };
        }
    }

    public List<Wallet> Wallets = new();
    public List<Wallet> GetWallets()
    {
        return Wallets.ToList();
    }
}