using System.Diagnostics;
using Blockchain.Models;
using Blockchain.Models.Metrics;

namespace Blockchain.Services;

public class MiningService
{
    private readonly HashService _hashService;

    public MiningService()
    {
        _hashService = new();
    }

    public MiningMetrics MineBlock(Block block, int difficulty, string? mask)
    {
        string target = mask ?? new string('0', difficulty);

        long attempts = 0;
        var stopwatch = Stopwatch.StartNew();

        while (true)
        {
            block.Hash = _hashService.ComputeHash(block);
            attempts++;

            if (block.Hash.StartsWith(target))
            {
                block.MiningDuration = stopwatch.Elapsed.TotalSeconds;
                block.Difficulty = difficulty;
                break;
            }

            block.Nonce++;
        }

        stopwatch.Stop();

        return new MiningMetrics
        {
            Attempts = attempts,
            ElapsedTime = stopwatch.Elapsed
        };
    }
}