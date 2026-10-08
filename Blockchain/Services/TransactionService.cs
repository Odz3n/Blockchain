using Blockchain.Models;
using Blockchain.Models.Validation;

namespace Blockchain.Services;

public class TransactionService
{
    private readonly WalletService _walletService = new();
    public Transaction CreateTransaction(
        Models.Type? type,
        Wallet sender,
        string to,
        decimal amount)
    {
        var transaction = new Transaction(type, sender.Address, to, amount, sender.PublicKey);
        transaction.Signature = sender.SignTransaction(transaction);

        var validationResult = ValidateTransaction(transaction);
        if (!validationResult.IsValid)
            throw new InvalidOperationException($"Invalid transaction: {validationResult.Message}");

        return transaction;
    }
    public ValidationResult ValidateTransaction(Transaction transaction)
    {
        if (transaction == null)
            return new ValidationResult 
            { 
                IsValid = false,
                Message = "Transaction is null." 
            };

        if (transaction.Type == null)
            return new ValidationResult
            {
                IsValid = false,
                Message = "Type is null."
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

        if (!_walletService.VerifyTransactionSignature(transaction, transaction.PublicKey))
        {
            return new ValidationResult
            {
                IsValid = false,
                Message = "Invalid transaction signature."
            };
        }

        return new ValidationResult
        {
            IsValid = true,
            Message = "Transaction is valid."
        };
    }
}