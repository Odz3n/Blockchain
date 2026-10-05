using Blockchain.Models;
using Blockchain.Models.Validation;

namespace Blockchain.Services;

public class TransactionService
{
    public Transaction CreateTransaction(
        string? from,
        string? to,
        decimal amount)
    {
        var tx = new Transaction(from, to, amount);
        var res = ValidateTransaction(tx);
        if (!res.IsValid)
            throw new InvalidOperationException($"Invalid transaction: {res.Message}");

        return new Transaction(from, to, amount);
    }
    public ValidationResult ValidateTransaction(Transaction transaction)
    {
        if (transaction == null)
            return new ValidationResult 
            { 
                IsValid = false,
                Message = "Transaction is null." 
            };

        if (string.IsNullOrWhiteSpace(transaction.From))
            return new ValidationResult
            {
                IsValid = false,
                Message = "Sender is required."
            };

        if (string.IsNullOrWhiteSpace(transaction.To))
            return new ValidationResult
            {
                IsValid = false,
                Message = "Recipient is required."
            };

        if (transaction.Amount <= 0)
            return new ValidationResult
            {
                IsValid = false,
                Message = "Transaction amount must be greater than zero."
            };

        return new ValidationResult
        {
            IsValid = true,
            Message = "Transaction is valid."
        };
    }
}