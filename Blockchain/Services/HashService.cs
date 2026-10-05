using Blockchain.Models;
using System.Security.Cryptography;
using System.Text;

namespace Blockchain.Services;

public class HashService
{
    public string ComputeHash(Block block)
    {
        string rawData = $"{block.Index}{block.Data}{block.Author}{block.Timestamp.ToString("o")}{block.PrevHash}{block.Nonce}{block.Difficulty}";

        return ComputeSHA256(rawData);
    }
    private string ComputeSHA256(string input)
    {
        var inputBytes = Encoding.UTF8.GetBytes(input);
        var hashBytes = SHA256.HashData(inputBytes);

        return Convert.ToBase64String(hashBytes);
    }
}
