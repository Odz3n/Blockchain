using Blockchain.Models;
using System.Diagnostics;

namespace Blockchain.Services;

public class BalanceService
{
    public decimal GetAvailableBalance(
        string address,
        List<Block> chain,
        List<Transaction> transactionPool)
    {
        decimal balance = GetBalance(address, chain);
        foreach (var trans in transactionPool)
        {
            if (trans.From == address)
                balance -= trans.Amount;
            if (trans.To == address)
                balance += trans.Amount;
        }
        return balance;
    }
    public decimal GetBalance(string address, List<Block> chain)
    {
        decimal balance = 0;
        foreach (var block in chain)
        {
            foreach (var trans in block.Transactions)
            {
                if (trans.From == address)
                    balance -= trans.Amount;
                if (trans.To == address)
                    balance += trans.Amount;
            }
        }
        return balance;
    }
    public bool HasSufficientBalance(
        string address,
        decimal amount,
        List<Block> chain,
        List<Transaction> transactionPool)
    {
        decimal balance = GetBalance(address, chain);
        foreach (var trans in transactionPool)
        {
            if (trans.From == address)
                balance -= trans.Amount;
            if (trans.To == address)
                balance += trans.Amount;
        }
        return balance >= amount;
    }
}
