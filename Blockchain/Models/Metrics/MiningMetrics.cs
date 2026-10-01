namespace Blockchain.Models.Metrics;

public class MiningMetrics
{
    public long Attempts { get; set; }
    public TimeSpan ElapsedTime { get; set; }
    public double HashRate =>
        ElapsedTime.TotalSeconds > 0 ? Attempts / ElapsedTime.TotalSeconds : 0;
}
