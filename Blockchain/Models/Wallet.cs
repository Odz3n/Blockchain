using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Blockchain.Models;

public class Wallet
{
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public byte[] PublicKey { get; set; }
    private byte[] PrivateKey { get; set; }
    public Wallet(
        string name,
        string address,
        byte[] publicKey,
        byte[] privateKey)
    {
        Name = name;
        Address = address;
        PublicKey = publicKey;
        PrivateKey = privateKey;
    }
    public byte[] SignTransaction(Transaction transaction)
    {
        using (var ecdsa = ECDsa.Create())
        {
            ecdsa.ImportECPrivateKey(PrivateKey, out int _);
            return ecdsa.SignData(transaction.GetDataToSign(), HashAlgorithmName.SHA256);
        }
    }
}
