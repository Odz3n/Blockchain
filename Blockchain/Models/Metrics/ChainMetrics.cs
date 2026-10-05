namespace Blockchain.Models.Metrics;

public class ChainMetrics
{
    public Block? FastestBlock { get; set; }
    public Block? SlowestBlock { get; set; }
    public Block? MostAttemptsBlock { get; set; }
    public double AvgMiningTime { get; set; }
    public double AvgAttemptsCount { get; set; }
    public int MaxDiffAtMining { get; set; }
    public int MinDiffAtMining { get; set; }
}
