namespace Blockchain.Models.Metrics;

public class NodeStatistics
{
    public int Blocks { get; set; }
    public int TransactionsInBlockchain { get; set; }
    public int MempoolCount { get; set; }
    public decimal PendingFees { get; set; }
    public double AverageMiningTime { get; set; }
    public decimal CurrentMiningReward { get; set; }
    public int NextHalvingBlock { get; set; }
    public int PeersCount { get; set; }
}
