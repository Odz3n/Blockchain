using Blockchain.Models;
using Blockchain.Models.Validation;
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
    public ValidationResult ValidateBalance(
        Transaction transaction,
        IReadOnlyDictionary<string, decimal> balances)
    {
        decimal senderBalance = balances.GetValueOrDefault(transaction.From, 0m);
        decimal recipientBalance = balances.GetValueOrDefault(transaction.To, 0m);

        if (senderBalance < 0 || recipientBalance < 0)
            return new ValidationResult
            {
                IsValid = false,
                Message = "Wallet balance cannot be negative."
            };

        if (senderBalance < transaction.Amount)
            return new ValidationResult
            {
                IsValid = false,
                Message = $"Sender {transaction.From} tries to spend {transaction.Amount} with balance {senderBalance}."
            };

        decimal newSenderBalance = senderBalance - transaction.Amount;
        decimal newRecipientBalance = recipientBalance + transaction.Amount;

        if (newSenderBalance < 0 || newRecipientBalance < 0)
            return new ValidationResult
            {
                IsValid = false,
                Message = $"Transaction {transaction.Id} results in a negative wallet balance."
            };

        return new ValidationResult
        {
            IsValid = true,
            Message = "Balance is valid."
        };
    }
    public void ApplyTransaction(
        Transaction transaction,
        Dictionary<string, decimal> balances)
    {
        // Apply for sender
        balances[transaction.From] = balances.GetValueOrDefault(transaction.From, 0m) - transaction.Amount;
        // Apply for recipient
        balances[transaction.To] = balances.GetValueOrDefault(transaction.To, 0m) + transaction.Amount;
    }
    public void ApplyCoinbase(
        Transaction coinbase,
        Dictionary<string, decimal> balances)
    {
        // Apply for miner
        balances[coinbase.To] = balances.GetValueOrDefault(coinbase.To, 0m) + coinbase.Amount;
    }
}