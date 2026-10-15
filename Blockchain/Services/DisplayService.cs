using Blockchain.Models;
using Blockchain.Models.Metrics;

namespace Blockchain.Services;

public class DisplayService
{
    private const int Width = 46;

    private readonly BalanceService _balanceService;
    private readonly BlockchainService _blockchainService;
    private readonly WalletService _walletService;

    public DisplayService(
        BalanceService balanceService,
        BlockchainService blockchainService,
        WalletService walletService)
    {
        _balanceService = balanceService;
        _blockchainService = blockchainService;
        _walletService = walletService;
    }

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

    public void DisplayTransactionHistory(List<Transaction> transactions)
    {
        DisplayTransactions(transactions);
    }

    public void DisplayMemPoolState(List<Transaction> mempool)
    {
        DisplayTransactions(mempool);
    }

    public void DisplayWalletStatement(Wallet wallet)
    {
        decimal balance = 0m;
        decimal initialBalance = 0m;
        decimal totalReceived = 0m;
        decimal totalSpent = 0m;
        decimal totalFeePaid = 0m;
        decimal totalFeeEarned = 0m;
        decimal miningRewards = 0m;

        Header($"WALLET STATEMENT [{wallet.Name}]");

        Row("Address", wallet.Address);
        Row("Initial Balance", $"{initialBalance:F2}");

        var blocks = _blockchainService.Chain
            .OrderBy(b => b.Index)
            .ToList();

        bool hasTransactions = false;
        foreach (var block in blocks)
        {
            var transactions = block.Transactions
                .Where(t => t.From == wallet.Address || t.To == wallet.Address)
                .OrderByDescending(t => t.IsCoinbase)
                .ToList();

            decimal blockFees = block.Author == wallet.Address
                ? block.Transactions
                    .Where(t => !t.IsCoinbase)
                    .Sum(t => t.Fee)
                : 0m;

            if (transactions.Count == 0 && blockFees == 0)
                continue;

            hasTransactions = true;
            Colored($"\n  BLOCK #{block.Index}\n", ConsoleColor.Cyan);
            Line();

            foreach (var trans in transactions)
            {
                decimal change = 0m;

                if (trans.IsCoinbase)
                {
                    change = trans.Amount;
                    miningRewards += trans.Amount;

                    Colored("  [MINING] ", ConsoleColor.Yellow);
                    Console.Write("COINBASE");
                    Colored($" +{trans.Amount:F2}\n", ConsoleColor.Green);
                }
                else
                {
                    string sender = _walletService.GetNameByAddress(trans.From) ?? "n/a";
                    string recipient = _walletService.GetNameByAddress(trans.To) ?? "n/a";

                    if (trans.To == wallet.Address)
                    {
                        change += trans.Amount;
                        totalReceived += trans.Amount;
                    }

                    if (trans.From == wallet.Address)
                    {
                        change -= trans.Amount + trans.Fee;
                        totalSpent += trans.Amount;
                        totalFeePaid += trans.Fee;
                    }

                    Colored(change > 0 ? "  [IN]  " : change < 0 ? "  [OUT] " : "  [SELF]",
                        change > 0 ? ConsoleColor.Green : change < 0 ? ConsoleColor.Red : ConsoleColor.DarkGray);
                    Colored($"  [{(trans.Type.HasValue ? trans.Type.Value.ToString() : "UNKNOWN").ToUpperInvariant()}] ",
                        ConsoleColor.DarkCyan);
                    Console.Write($"{sender} -> {recipient}");
                    Colored($"  {change:+0.00;-0.00;0.00}",
                        change > 0 ? ConsoleColor.Green : change < 0 ? ConsoleColor.Red : ConsoleColor.DarkGray);
                    Colored($"  [FEE {trans.Fee:F2}]\n",
                        ConsoleColor.DarkGray);
                }

                balance += change;

                Colored("  Balance: ", ConsoleColor.DarkGray);
                Colored($"{balance:F2}\n", ConsoleColor.White);
            }

            if (blockFees > 0)
            {
                balance += blockFees;
                totalFeeEarned += blockFees;

                Colored("  [MINING] ", ConsoleColor.Yellow);
                Console.Write("TRANSACTION FEES");
                Colored($" +{blockFees:F2}\n", ConsoleColor.Green);
                Colored("  Balance: ", ConsoleColor.DarkGray);
                Colored($"{balance:F2}\n", ConsoleColor.White);
            }
        }

        if (!hasTransactions)
            Colored("\n  No transactions found.\n", ConsoleColor.Yellow);

        Header("WALLET STATEMENT SUMMARY");
        Row("Initial Balance", $"{initialBalance:F2}");
        Colored("  Total Received     : ", ConsoleColor.DarkGray);
        Colored($"+{totalReceived:F2}\n", ConsoleColor.Green);
        Colored("  Total Spent        : ", ConsoleColor.DarkGray);
        Colored($"-{totalSpent:F2}\n", ConsoleColor.Red);
        Colored("  Total Fees Paid    : ", ConsoleColor.DarkGray);
        Colored($"-{totalFeePaid:F2}\n", ConsoleColor.Red);
        Colored("  Total Fees Earned  : ", ConsoleColor.DarkGray);
        Colored($"+{totalFeeEarned:F2}\n", ConsoleColor.Green);
        Colored("  Mining Rewards     : ", ConsoleColor.DarkGray);
        Colored($"+{miningRewards:F2}\n", ConsoleColor.Yellow);
        Line();
        Colored("  FINAL BALANCE : ", ConsoleColor.Cyan);
        Colored($"{balance:F2}\n", ConsoleColor.White);
        Line();
    }

    public void DisplayWallets(IEnumerable<Wallet> wallets)
    {
        foreach (var wallet in wallets)
            DisplayWallet(wallet);
    }

    public void DisplayWallet(Wallet? wallet)
    {
        if (wallet == null)
        {
            Colored("  Wallet is null.\n", ConsoleColor.Yellow);
            return;
        }

        Header("WALLETS");

        Row("Name", wallet.Name);
        Row("Address", wallet.Address);
        Row("PubKey", Convert.ToBase64String(wallet.PublicKey));
        Row("Balance", _balanceService.GetAvailableBalance(wallet.Address, _blockchainService.Chain, _blockchainService.MemPool).ToString());
    }

    public void DisplayTransaction(Transaction? transaction)
    {
        if (transaction == null)
        {
            Colored("  Transaction is null.\n", ConsoleColor.Yellow);
            return;
        }

        Header("TRANSACTION");

        Row("Id", transaction.Id);
        Row("Type", transaction.Type.ToString()!);
        Row("From", transaction.From);
        Row("To", transaction.To);
        Row("Amount", transaction.Amount.ToString("F2"));
        Row("Fee", transaction.Fee.ToString("F2"));
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