using Blockchain.Models;
using Blockchain.Models.Metrics;

namespace Blockchain.Services;

public class DisplayService
{
    private const int Width = 46;

    public void Display(List<Block> chain)
    {
        Header($"BLOCKCHAIN [{chain.Count} blocks]");

        if (chain.Count == 0)
        {
            Colored("  Blockchain is empty.\n", ConsoleColor.Yellow);
            return;
        }

        foreach (var block in chain)
            DisplayBlock(block);
    }

    public void DisplayBlock(Block block)
    {
        Colored(
            $"\n  #{block.Index} {(block.Index == 0 ? "[GENESIS]" : "")}\n",
            ConsoleColor.Cyan);

        Row("Author", block.Author);
        Row("Time", block.Timestamp.ToString("yyyy-MM-dd HH:mm:ss"));
        Row("Nonce", $"{block.Nonce:N0}");
        Row("Hash", block.Hash);
        Row("PrevHash", block.PrevHash);
        Row("Difficulty", block.Difficulty.ToString());
        Row("MiningDuration", block.MiningDuration.ToString());
        Row("Transactions", block.Transactions.Count.ToString());
        if (block.Transactions != null && block.Transactions.Count > 0)
        {
            for (int i = 0; i < block.Transactions.Count; i++)
            {
                Row($"Transaction #{i}", block.Transactions[i].ToString());
            }
        }

        Line();
    }

    public void DisplayTransaction(Transaction? transaction)
    {
        if (transaction == null)
        {
            Colored("  Transaction is null.\n", ConsoleColor.Yellow);
            return;
        }

        Header("TRANSACTION");

        Row("Type", transaction.Type.ToString()!);
        Row("From", transaction.From);
        Row("To", transaction.To);
        Row("Amount", transaction.Amount.ToString("F2"));
    }

    public void DisplayTransactions(IEnumerable<Transaction> transactions)
    {
        foreach (var transaction in transactions)
            DisplayTransaction(transaction);
    }

    public void DisplayDifficultyChangeMetrics(DifficultyChangeMetrics metrics)
    {
        Header("DIFFICULTY CHANGE METRICS");

        Row("Avg time", $"{metrics.AvgMiningTime:N0}");
        Row("Target block time", $"{metrics.TargetBlockTime:N0}");
        Row("Old difficulty", $"{metrics.OldDifficulty:N0}");
        Row("New difficulty", $"{metrics.NewDifficulty:N0}");
        Row("Reason", $"{metrics.Reason:N0}");
    }

    public void DisplayMiningMetrics(MiningMetrics metrics)
    {
        Header("MINING METRICS");

        Row("Attempts", $"{metrics.Attempts:N0}");
        Row("Elapsed", FormatTime(metrics.ElapsedTime));
        Row("Hash Rate", $"{metrics.HashRate:N0} H/s");
    }

    public void DisplayRepairMetrics(ChainRepairMetrics metrics)
    {
        Header("REPAIR METRICS");

        Row("Blocks", $"{metrics.BlocksRepaired:N0}");
        Row("Attempts", $"{metrics.TotalAttempts:N0}");
        Row("Mining Time", FormatTime(metrics.TotalMiningTime));
        Row("Total Time", FormatTime(metrics.TotalElapsedTime));
        Row("Avg Attempts", $"{metrics.AverageAttemptsPerBlock:N0}");
        Row("Avg Time", $"{metrics.AverageTimePerBlock:F3}s");
        Row("Hash Rate", $"{metrics.AverageHashRate:N0} H/s");
    }

    private void Header(string title)
    {
        Colored($"\n  {title}\n", ConsoleColor.Cyan);
        Line();
    }

    private void Row(string label, string value)
    {
        Colored($"  {label,-14}: ", ConsoleColor.DarkGray);
        Console.WriteLine(value);
    }

    private void Line()
    {
        Colored(new string('-', Width) + "\n", ConsoleColor.DarkGray);
    }

    private void Colored(string text, ConsoleColor color)
    {
        var previous = Console.ForegroundColor;
        Console.ForegroundColor = color;
        Console.Write(text);
        Console.ForegroundColor = previous;
    }

    private static string FormatTime(TimeSpan time) =>
        time.TotalSeconds >= 1
            ? $"{time.TotalSeconds:F3}s"
            : $"{time.TotalMilliseconds:F2}ms";
}