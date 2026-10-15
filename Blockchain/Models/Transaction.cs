using System.Text;

namespace Blockchain.Models;

public enum Type
{
    Transfer,
    Purchase,
    Gift,
    Coinbase
}
public class Transaction
{
    public string Id { get; set; }
    public Type? Type { get; set; }
    public string From { get; set; } = string.Empty;
    public string To { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal Fee { get; set; }
    public DateTime Timestamp { get; set; }
    public byte[] Signature { get; set; } = Array.Empty<byte>();
    public byte[] PublicKey { get; set; }
    public bool IsCoinbase { get; set; }
    public Transaction(
        Type? type,
        string from,
        string to,
        decimal amount,
        decimal fee,
        byte[] publicKey)
    {
        Id = Guid.NewGuid().ToString();
        Type = type;
        From = from;
        To = to;
        Amount = amount;
        Fee = fee;
        Timestamp = DateTime.UtcNow;
        PublicKey = publicKey;
        IsCoinbase = false;
    }
    public byte[] GetDataToSign()
    {
        return Encoding.UTF8.GetBytes(ToRawString());
    }
    public string ToRawString()
    {
        return $"{Id}\n{Type}\n{From}\n{To}\n{Amount}\n{Fee}\n{Timestamp}";
    }
    public override string ToString()
    {
        return $"\n\t - Id: {Id}\n\t - Type: {Type}\n\t - From: {From}\n\t - To: {To}\n\t - Amount: {Amount}\n\t - Fee: {Fee}\n\t - Timestamp: {Timestamp}\n";
    }
}