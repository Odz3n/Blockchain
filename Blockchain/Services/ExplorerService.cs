using Blockchain.Models;

namespace Blockchain.Services;

public class ExplorerService
{
    private readonly BlockchainService _blockchainService;
    public ExplorerService(
        BlockchainService blockchainService)
    {
        _blockchainService = blockchainService;
    }

    public Transaction? FindById(string Id)
    {
        return GetTransactions()
            .FirstOrDefault(t => t.Id == Id);
    }
    public List<Transaction> GetTransactionsBySender(string sender)
    {
        return GetTransactions()
            .Where(t => t.From == sender)
            .ToList();
    }
    public List<Transaction> GetTransactionsWithAmountGreaterThan(decimal amount)
    {
        return GetTransactions()
            .Where(t => t.Amount > amount)
            .ToList();
    }
    public Transaction? GetTheBiggestTransaction()
    {
        return GetTransactions()
            .OrderByDescending(t => t.Amount)
            .FirstOrDefault();
    }
    private IEnumerable<Transaction> GetTransactions()
    {
        return _blockchainService.Chain
            .SelectMany(b => b.Transactions);
    }
}