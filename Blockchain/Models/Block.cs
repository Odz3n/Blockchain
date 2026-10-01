namespace Blockchain.Models;

public class Block
{
    // Block's id
    public int Index { get; set; }
    // Block's creation time
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    // Block's author
    public string Author { get; set; } = null!;
    // Block's data
    public string Data { get; set; } = null!;
    // Block's hash
    public string Hash { get; set; } = null!;
    // Block's prev block hash
    public string PrevHash { get; set; } = null!;
    public long Nonce { get; set; } = 0;
}
