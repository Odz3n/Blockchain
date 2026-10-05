namespace Blockchain.Models.Metrics;

public class DifficultyChangeMetrics
{
    public double AvgMiningTime { get; set; }
    public double TargetBlockTime { get; set; }
    public int OldDifficulty { get; set; }
    public int NewDifficulty { get; set; }
    public string Reason { get; set; } = string.Empty;
}
