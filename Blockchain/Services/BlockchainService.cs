using Blockchain.Models;
using Blockchain.Models.Metrics;
using Blockchain.Models.Validation;
using System.Diagnostics;

namespace Blockchain.Services;

public class BlockchainService
{
    private readonly HashService _hashService;
    private readonly MiningService _miningService;

    public List<Block> Chain { get; set; } = new();

    public int Difficulty { get; private set; } = 4;
    public string HashMask { get; private set; } = string.Empty;

    private readonly double _targetBlockTime = 2;
    private readonly int _adjustmentInterval = 2;
    private readonly int _minDifficulty = 1;
    private readonly int _maxDifficulty = 6;

    public BlockchainService(
        int difficulty = 1,
        double targetBlockTime = 2,
        int adjustmentInterval = 2)
    {
        _hashService = new();
        _miningService = new();

        _adjustmentInterval = adjustmentInterval;
        _targetBlockTime = targetBlockTime;

        Difficulty = difficulty;
        HashMask = new string('0', Difficulty);

        AddGenesisBlock();
    }
    public DifficultyChangeMetrics? AddBlock(string data, string author)
    {
        Block lastBlock = Chain[Chain.Count - 1];

        var newBlock = new Block
        {
            Index = lastBlock.Index + 1,
            Data = data,
            Author = author,
            PrevHash = lastBlock.Hash,
            Difficulty = Difficulty
        };

        _miningService.MineBlock(newBlock, Difficulty, HashMask);

        Chain.Add(newBlock);

        if (newBlock.Index % _adjustmentInterval == 0)
            return AdjustDifficulty();
        return null;
    }
    private DifficultyChangeMetrics AdjustDifficulty()
    {
        var recentBlocks = Chain
            .Where(b => b.Index > 0)
            .TakeLast(_adjustmentInterval)
            .ToList();

        if (recentBlocks.Count == 0)
            throw new InvalidOperationException(nameof(recentBlocks.Count));

        var avgTime = recentBlocks.Average(b => b.MiningDuration);

        var metrics = new DifficultyChangeMetrics
        {
            TargetBlockTime = _targetBlockTime,
            AvgMiningTime = avgTime,
            OldDifficulty = Difficulty
        };

        if (avgTime < _targetBlockTime * 0.25)
        {
            ChangeDifficulty(Math.Min(_maxDifficulty, Difficulty + 2));
            metrics.Reason = "Average mining time below 25% of target.";
        }
        else if (avgTime < _targetBlockTime * 0.5)
        {
            ChangeDifficulty(Math.Min(_maxDifficulty, Difficulty + 1));
            metrics.Reason = "Average mining time below 50% of target.";
        }
        else if (avgTime > _targetBlockTime * 4)
        {
            ChangeDifficulty(Math.Max(_minDifficulty, Difficulty - 2));
            metrics.Reason = "Average mining time above 400% of target.";
        }
        else if (avgTime > _targetBlockTime * 2)
        {
            ChangeDifficulty(Math.Max(_minDifficulty, Difficulty - 1));
            metrics.Reason = "Average mining time above 200% of target.";
        }
        else
        {
            metrics.Reason = "Nothing changed.";
        }

        metrics.NewDifficulty = Difficulty;

        return metrics;
    }
    public ValidationResult IsValid()
    {
        for (int i = 1; i < Chain.Count; i++)
        {
            var currentBlock = Chain[i];
            var prevBlock = Chain[i - 1];

            string currentHash = _hashService.ComputeHash(currentBlock);

            if (currentBlock.Hash != currentHash)
                return new ValidationResult 
                { 
                    IsValid = false,
                    Message = $"Invalid block: {Chain[i].Index}. Invalid Hash."
                };

            if (prevBlock.Hash != currentBlock.PrevHash)
                return new ValidationResult
                {
                    IsValid = false,
                    Message = $"Invalid block: {Chain[i].Index}. Invalid prevHash."
                };

            var target = new string('0', currentBlock.Difficulty);
            if (!currentBlock.Hash.StartsWith(string.IsNullOrEmpty(HashMask) ? target : HashMask))
                return new ValidationResult 
                {
                    IsValid = false,
                    Message = $"Invalid block: {Chain[i].Index}. Invalid Hash Mask."
                };
        }
        return new ValidationResult
        {
            IsValid = true,
            Message = "Blockchain is valid."
        };
    }
    public ChainMetrics GetChainMetrics()
    {
        double minMiningDuration = Chain.Min(b => b.MiningDuration);
        double maxMiningDuration = Chain.Max(b => b.MiningDuration);
        int maxDiff = Chain.Max(b => b.Difficulty);
        int minDiff = Chain.Min(b => b.Difficulty);
        long attemptsCount = Chain.Max(b => b.Nonce);

        return new ChainMetrics
        {
            FastestBlock = Chain.FirstOrDefault(b => b.MiningDuration <= minMiningDuration),
            SlowestBlock = Chain.FirstOrDefault(b => b.MiningDuration >= maxMiningDuration),
            MostAttemptsBlock = Chain.FirstOrDefault(b => b.Nonce >= attemptsCount),
            AvgMiningTime = Chain.Average(b => b.MiningDuration),
            AvgAttemptsCount = Chain.Average(b => b.Nonce),
            MaxDiffAtMining = maxDiff,
            MinDiffAtMining = minDiff
        };
    }
    public void ChangeDifficulty(int difficulty)
    {
        if (difficulty < 0 || difficulty > 64)
            throw new ArgumentOutOfRangeException(nameof(difficulty));

        Difficulty = difficulty;
        HashMask = new string('0', difficulty);
    }
    public void ChangeHashMask(string mask)
    {
        if (string.IsNullOrEmpty(mask))
            throw new ArgumentException("Mask cannot be empty.");

        if (mask.Length > 64)
            throw new ArgumentException("Mask is too long.");

        if (mask.Any(c => !Uri.IsHexDigit(c)))
            throw new ArgumentException("Mask must contain hexadecimal characters.");

        HashMask = mask.ToLowerInvariant();
        Difficulty = HashMask.Length;
    }
    public void ChangeData(Block? block, string? data)
    {
        // validate
        ArgumentNullException.ThrowIfNull(block);
        ArgumentException.ThrowIfNullOrWhiteSpace(data);

        if (!Chain.Contains(block))
            throw new ArgumentException("Block does not belong to this blockchain.", nameof(block));

        // change data explicitly
        block.Data = data;

        // re-mine it to get correct hash
        RemineBlock(block);
    }
    public MiningMetrics RemineBlock(Block? block)
    {
        ArgumentNullException.ThrowIfNull(block);

        int index = Chain.IndexOf(block);

        if (index < 0 || index > Chain.Count - 1)
            throw new ArgumentException("Block does not belong to this blockchain.", nameof(block));

        if (index > 0)
            block.PrevHash = Chain[index - 1].Hash;

        block.Nonce = default;

        return _miningService.MineBlock(block, Difficulty, HashMask);
    }
    public ChainRepairMetrics RepairChain(int startIndex = 0)
    {
        if (startIndex < 0 || startIndex >= Chain.Count)
            throw new ArgumentOutOfRangeException(nameof(startIndex));

        var metrics = new ChainRepairMetrics();
        var stopwatch = Stopwatch.StartNew();

        for (int i = startIndex; i < Chain.Count; i++)
        {
            var miningMetrics = RemineBlock(Chain[i]);

            metrics.BlocksRepaired++;
            metrics.TotalAttempts += miningMetrics.Attempts;
            metrics.TotalMiningTime += miningMetrics.ElapsedTime;
        }

        stopwatch.Stop();

        metrics.TotalElapsedTime = stopwatch.Elapsed;

        return metrics;
    }
    private void AddGenesisBlock()
    {
        var genesis = new Block
        {
            Index = 0,
            Data = "0",
            Timestamp = DateTime.Parse("1900-01-01"),
            Author = "0",
            PrevHash = "0"
        };

        _miningService.MineBlock(genesis, Difficulty, HashMask);

        Chain.Add(genesis);
    }
}
