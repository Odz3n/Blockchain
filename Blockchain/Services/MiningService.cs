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
                break;

            block.Nonce++;

            //if (block.Nonce % 100_000 == 0)
            //{
            //    Console.WriteLine(
            //        $"Mining in progress..." +
            //        $"\n\tCurrent Nonce: {block.Nonce}" +
            //        $"\n\tCurrent Hash: {block.Hash}" +
            //        $"\n\tElapsed Time: {stopwatch.Elapsed}");
            //}
        }

        stopwatch.Stop();

        return new MiningMetrics
        {
            Attempts = attempts,
            ElapsedTime = stopwatch.Elapsed
        };
    }
}