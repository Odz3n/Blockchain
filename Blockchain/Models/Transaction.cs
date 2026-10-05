namespace Blockchain.Models;

public enum Type
{
    Transfer,
    Purchase,
    Gift
}
public class Transaction
{
    public string Id { get; set; }
    public Type? Type { get; set; }
    public string From { get; set; } = string.Empty;
    public string To { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime Timestamp { get; set; }
    public Transaction(
        Type? type,
        string from,
        string to,
        decimal amount)
    {
        Id = Guid.NewGuid().ToString();
        Type = type;
        From = from;
        To = to;
        Amount = amount;
        Timestamp = DateTime.UtcNow;
    }
    public string ToRawString()
    {
        return $"{Id}\n{Type}\n{From}\n{To}\n{Amount}\n{Timestamp}";
    }
    public override string ToString()
    {
        return $"\n\t - Id: {Id}\n\t - Type: {Type}\n\t - From: {From}\n\t - To: {To}\n\t - Amount: {Amount}\n\t - Timestamp: {Timestamp}\n";
    }
}