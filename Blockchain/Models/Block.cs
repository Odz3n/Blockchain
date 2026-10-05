namespace Blockchain.Models;

public class Block
{
    public int Index { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string Author { get; set; } = null!;
    public List<Transaction> Transactions { get; set; }
    public string Hash { get; set; } = null!;
    public string PrevHash { get; set; } = null!;
    public long Nonce { get; set; } = 0;
    public int Difficulty { get; set; }
    public double MiningDuration { get; set; }
}
