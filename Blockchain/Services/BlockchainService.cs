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
    public string HashMask { get; private set; }

    public BlockchainService(int difficulty = 5)
    {
        _hashService = new();
        _miningService = new();

        Difficulty = difficulty;
        HashMask = new string('0', Difficulty);

        AddGenesisBlock();
    }
    public void AddBlock(string data, string author)
    {
        Block lastBlock = Chain[Chain.Count - 1];

        var newBlock = new Block
        {
            Index = lastBlock.Index + 1,
            Data = data,
            Author = author,
            PrevHash = lastBlock.Hash
        };

        _miningService.MineBlock(newBlock, Difficulty, HashMask);

        Chain.Add(newBlock);
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

            if (!currentBlock.Hash.StartsWith(HashMask))
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
