using Blockchain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Blockchain.Services;

public class WalletService
{
    public Wallet CreateWallet(string name)
    {
        using (var rsa = ECDsa.Create(ECCurve.NamedCurves.nistP256))
        {
            var privateKey = rsa.ExportECPrivateKey();
            var publicKey = rsa.ExportSubjectPublicKeyInfo();

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

    public List<Wallet> Wallets = new();
    public List<Wallet> GetWallets()
    {
        return Wallets.ToList();
    }
}
