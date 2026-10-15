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
                balance -= trans.Amount + trans.Fee;
            // Child Pays for Parent(CPFP) case
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
                    balance -= trans.Amount + trans.Fee;
                if (trans.To == address)
                    balance += trans.Amount;

                if (!trans.IsCoinbase && block.Author == address)
                    balance += trans.Fee;
            }
        }
        return balance;
    }
    public bool HasSufficientBalance(
        Transaction transaction,
        List<Block> chain,
        List<Transaction> transactionPool)
    {
        decimal balance = GetBalance(transaction.From, chain);
        foreach (var trans in transactionPool)
        {
            if (trans.From == transaction.From)
                balance -= trans.Amount + trans.Fee;
            if (trans.To == transaction.From)
                balance += trans.Amount;
        }
        return balance >= transaction.Amount + transaction.Fee;
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

        if (senderBalance < transaction.Amount + transaction.Fee)
            return new ValidationResult
            {
                IsValid = false,
                Message = $"Sender {transaction.From} tries to spend {transaction.Amount + transaction.Fee} (fee {transaction.Fee} included) with balance {senderBalance}."
            };

        decimal newSenderBalance = senderBalance - (transaction.Amount + transaction.Fee);
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
        balances[transaction.From] = balances.GetValueOrDefault(transaction.From, 0m) - (transaction.Amount + transaction.Fee);
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
    public void ApplyMiningFees(
        Block block,
        Dictionary<string, decimal> balances)
    {
        decimal totalFees = block.Transactions
            .Where(t => !t.IsCoinbase)
            .Sum(t => t.Fee);

        balances[block.Author] = balances.GetValueOrDefault(block.Author, 0m) + totalFees;
    }
}