namespace Blockchain.Models.Metrics;

public class MiningMetrics
{
    public long Attempts { get; set; }
    public TimeSpan ElapsedTime { get; set; }

    public int AcceptedTransactions { get; set; }
    public int RejectedTransactions { get; set; }
    public int RemainingMempoolTransactions { get; set; }

    public decimal TotalFees { get; set; }
    public decimal MiningReward { get; set; }
    public decimal MinerReceived => MiningReward + TotalFees;

    public double HashRate =>
        ElapsedTime.TotalSeconds > 0
            ? Attempts / ElapsedTime.TotalSeconds
            : 0;
}