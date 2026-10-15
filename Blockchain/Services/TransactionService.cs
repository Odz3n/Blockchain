using Blockchain.Models;
using Blockchain.Models.Validation;

namespace Blockchain.Services;

public class TransactionService
{
    private readonly WalletService _walletService = new();
    private readonly BalanceService _balanceService = new();
    public Transaction CreateTransaction(
        Models.Type? type,
        Wallet sender,
        string to,
        decimal amount)
    {
        if (type == Models.Type.Coinbase)
            throw new InvalidOperationException("Coinbase transactions cannot be created manually.");

        var transaction = new Transaction(type, sender.Address, to, amount, sender.PublicKey);
        transaction.Signature = sender.SignTransaction(transaction);

        var validationResult = ValidateTransaction(transaction);
        if (!validationResult.IsValid)
            throw new InvalidOperationException($"Invalid transaction: {validationResult.Message}");

        return transaction;
    }
    public Transaction CreateCoinbaseTransaction(
        string minerAddress,
        decimal reward)
    {
        var transaction = new Transaction(Models.Type.Coinbase, "SYSTEM", minerAddress, reward, null)
        {
            IsCoinbase = true
        };
        return transaction;
    }
    public ValidationResult ValidateTransaction(
        Transaction transaction,
        bool includeSignatureValidation = true)
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

        if (transaction.IsCoinbase || transaction.Type == Models.Type.Coinbase)
            return new ValidationResult
            {
                IsValid = false,
                Message = "Coinbase transactions require separate validation."
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

        if (transaction.From == transaction.To)
            return new ValidationResult
            {
                IsValid = false,
                Message = "Sender and recipient cannot be the same."
            };

        if (transaction.Amount <= 0)
            return new ValidationResult
            {
                IsValid = false,
                Message = "Transaction amount must be greater than zero."
            };

        if (includeSignatureValidation)
        {
            if (!_walletService.VerifyTransactionSignature(transaction, transaction.PublicKey))
            {
                return new ValidationResult
                {
                    IsValid = false,
                    Message = "Invalid transaction signature."
                };
            }
        }

        return new ValidationResult
        {
            IsValid = true,
            Message = "Transaction is valid."
        };
    }
    public ValidationResult ValidateTransactionWithBalance(
        Transaction transaction,
        List<Block> chain,
        List<Transaction> transactionPool)
    {
        if (transactionPool.Contains(transaction))
            transactionPool.Remove(transaction);

        var validationResult = ValidateTransaction(transaction);
        if (!validationResult.IsValid)
            return new ValidationResult
            {
                IsValid = validationResult.IsValid,
                Message = validationResult.Message
            };
        bool hasSufficientBalance = _balanceService.HasSufficientBalance(transaction.From, transaction.Amount, chain, transactionPool);
        if (!hasSufficientBalance)
            return new ValidationResult { IsValid = false, Message = "Insufficient balance for the transaction." };
        return new ValidationResult { IsValid = true, Message = "Transaction is valid and has sufficient balance." };
    }
    public ValidationResult ValidateCoinbase(
        Block block,
        decimal miningReward)
    {
        var coinbases = block.Transactions
            .Where(t => t.IsCoinbase == true || t.Type == Models.Type.Coinbase)
            .ToList();

        if (coinbases.Count != 1)
            return new ValidationResult
            {
                IsValid = false,
                BlockIndex = block.Index,
                Message = $"Expected 1 coinbase transaction, found {coinbases.Count}."
            };

        var coinbase = coinbases.First();

        if (!coinbase.IsCoinbase || coinbase.Type != Models.Type.Coinbase)
            return new ValidationResult
            {
                IsValid = false,
                BlockIndex = block.Index,
                Message = "Invalid coinbase type or flag."
            };

        if (coinbase.From != "SYSTEM")
            return new ValidationResult
            {
                IsValid = false,
                BlockIndex = block.Index,
                Message = "Coinbase sender must be SYSTEM."
            };

        if (coinbase.To != block.Author)
            return new ValidationResult
            {
                IsValid = false,
                BlockIndex = block.Index,
                Message = "Coinbase recipient must be the block miner."
            };

        if (coinbase.Amount != miningReward)
            return new ValidationResult
            {
                IsValid = false,
                BlockIndex = block.Index,
                Message = $"Invalid mining reward. Expected {miningReward}, got {coinbase.Amount}."
            };

        return new ValidationResult { IsValid = true, Message = "Coinbase transaction is valid." };
    }
}