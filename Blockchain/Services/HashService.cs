using Blockchain.Models;
using System.Security.Cryptography;
using System.Text;

namespace Blockchain.Services;

public class HashService
{
    public string ComputeHash(Block block)
    {
        var transactionsHash = ComputeMerkleRoot(block.Transactions);
        string rawData = $"{block.Index}{transactionsHash}{block.Author}{block.Timestamp.ToString("o")}{block.PrevHash}{block.Nonce}{block.Difficulty}";

        return ComputeSHA256(rawData);
    }
    public string ComputeSHA256(string input)
    {
        var inputBytes = Encoding.UTF8.GetBytes(input);
        var hashBytes = SHA256.HashData(inputBytes);

        return Convert.ToBase64String(hashBytes);
    }
    public string ComputeMerkleRoot(List<Transaction> transactions)
    {
        if (transactions.Count == 0)
            return string.Empty;

        List<string> markleLeaves = new List<string>();
        foreach (var transaction in transactions)
            markleLeaves.Add(ComputeSHA256(transaction.ToRawString()));

        while (markleLeaves.Count > 1)
        {
            List<string> newLevel = new List<string>();
            for (int i = 0; i < markleLeaves.Count; i+=2)
            {
                if (i + 1 < markleLeaves.Count)
                    newLevel.Add(ComputeSHA256(markleLeaves[i] + markleLeaves[i + 1]));
                else
                    newLevel.Add(markleLeaves[i] + markleLeaves[i]);
            }
            markleLeaves = newLevel;
        }
        return markleLeaves[0];
    }
}
