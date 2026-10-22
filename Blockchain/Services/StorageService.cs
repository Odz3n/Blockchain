using Blockchain.Models;
using System.Text.Json;

namespace Blockchain.Services;

public class StorageService
{
    // Blockchain
    public async Task SaveAsync(BlockchainService blockchain)
    {
        var state = new BlockchainState
        {
            Chain = blockchain.Chain,
            MemPool = blockchain.MemPool,
            Difficulty = blockchain.Difficulty
        };

        var json = JsonSerializer.Serialize(state);
        await File.WriteAllTextAsync(Path.Combine(Directory.GetCurrentDirectory(), "save.json"), json);
    }
    // Wallets
    public async Task SaveAsync(Wallet wallet)
    {
        var state = new WalletState
        {
            Name = wallet.Name,
            Address = wallet.Address,
            PublicKey = wallet.PublicKey
        };

        var directory = Path.Combine(
            Directory.GetCurrentDirectory(),
            "Wallets",
            state.Address);
        Directory.CreateDirectory(directory);

        var json = JsonSerializer.Serialize(state);
        await File.WriteAllTextAsync(Path.Combine(directory, "wallet.json"), json);
    }
    public async Task<T> LoadAsync<T>(string path)
    {
        var json = await File.ReadAllTextAsync(path);
        return JsonSerializer.Deserialize<T>(json)
            ?? throw new InvalidDataException($"Invalid {nameof(T)} state.");
    }
}
