namespace Blockchain.Models.Metrics;

public class ChainRepairMetrics
{
    public int BlocksRepaired { get; set; }
    public long TotalAttempts { get; set; }

    public TimeSpan TotalMiningTime { get; set; }
    public TimeSpan TotalElapsedTime { get; set; }

    public double AverageAttemptsPerBlock =>
        BlocksRepaired > 0 ? (double)TotalAttempts / BlocksRepaired : 0;

    public double AverageTimePerBlock =>
        BlocksRepaired > 0 ? TotalMiningTime.TotalSeconds / BlocksRepaired : 0;

    public double AverageHashRate =>
        TotalMiningTime.TotalSeconds > 0 ? TotalAttempts / TotalMiningTime.TotalSeconds : 0;
}