namespace Blockchain.Models;

public class BlockchainState
{
    public List<Block> Chain { get; set; } = new();
    public List<Transaction> MemPool { get; set; } = new();
    public int Difficulty { get; set; }
}
