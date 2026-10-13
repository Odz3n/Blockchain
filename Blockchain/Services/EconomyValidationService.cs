using Blockchain.Models;
using Blockchain.Models.Validation;

namespace Blockchain.Services;

public class EconomyValidationService
{
    private readonly BlockchainService _blockchainService;
    private readonly TransactionService _transactionService;
    private readonly WalletService _walletService;



    public EconomyValidationService(
        BlockchainService blockchainService,
        TransactionService transactionService,
        WalletService walletService)
    {
        _blockchainService = blockchainService;
        _transactionService = transactionService;
        _walletService = walletService;
    }

    public ValidationResult ValidateEconomy()
    {
        var integrityResult = ValidateIntegrity();
        if (!integrityResult.IsValid)
            return integrityResult;

        var balances = new Dictionary<string, decimal>();
        foreach (var block in _blockchainService.Chain.OrderBy(b => b.Index))
        {
            var coinbaseResult = ValidateCoinbaseTransactions(block);
            if (!coinbaseResult.IsValid)
                return coinbaseResult;

            var regularResult = ValidateRegularTransactions(block, balances);
            if (!regularResult.IsValid)
                return regularResult;

            var coinbase = block.Transactions.FirstOrDefault(t => t.IsCoinbase);
            if (coinbase != null)
                balances[coinbase.To] = balances.GetValueOrDefault(coinbase.To, 0m) + coinbase.Amount;
        }

        return new ValidationResult
        {
            IsValid = true,
            Message = "Blockchain economy is valid."
        };
    }
    private ValidationResult ValidateIntegrity()
    {
        return _blockchainService.Validate();
    }
    private ValidationResult ValidateCoinbaseTransactions(Block block)
    {
        var coinbaseTransactions = block.Transactions
            .Where(t => t.IsCoinbase || t.Type == Models.Type.Coinbase)
            .ToList();

        if (block.Index == 0 && coinbaseTransactions.Count == 0)
            return new ValidationResult
            {
                IsValid = true,
                BlockIndex = block.Index,
                Message = "Genesis block is valid."
            };

        if (coinbaseTransactions.Count != 1)
            return new ValidationResult
            {
                IsValid = false,
                BlockIndex = block.Index,
                Message = $"Expected 1 coinbase transaction, found {coinbaseTransactions.Count}."
            };

        var coinbase = coinbaseTransactions[0];
        if (!coinbase.IsCoinbase || coinbase.Type != Models.Type.Coinbase)
            return new ValidationResult
            {
                IsValid = false,
                BlockIndex = block.Index,
                Message = "Coinbase transaction has an invalid type or flag."
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

        if (coinbase.Amount != _blockchainService.MiningReward)
            return new ValidationResult
            {
                IsValid = false,
                BlockIndex = block.Index,
                Message = $"Invalid mining reward. Expected {_blockchainService.MiningReward}, got {coinbase.Amount}."
            };

        return new ValidationResult
        {
            IsValid = true,
            BlockIndex = block.Index,
            Message = "Coinbase transaction is valid."
        };
    }
    private ValidationResult ValidateRegularTransactions(
        Block block,
        Dictionary<string, decimal> balances)
    {
        foreach (var trans in block.Transactions
            .Where(t => !t.IsCoinbase && t.Type != Models.Type.Coinbase))
        {
            var validationResult = _transactionService.ValidateTransaction(trans);
            if (!validationResult.IsValid)
                return new ValidationResult
                {
                    IsValid = false,
                    BlockIndex = block.Index,
                    Message = validationResult.Message
                };

            decimal senderBalance = balances.GetValueOrDefault(trans.From, 0m);
            decimal recipientBalance = balances.GetValueOrDefault(trans.To, 0m);

            if (senderBalance < trans.Amount)
                return new ValidationResult
                {
                    IsValid = false,
                    BlockIndex = block.Index,
                    Message = $"{_walletService.GetNameByAddress(trans.From)} tries to spend {trans.Amount} with balance {senderBalance}."
                };

            decimal newSenderBalance = senderBalance - trans.Amount;
            decimal newRecipientBalance = recipientBalance + trans.Amount;

            if (newSenderBalance < 0 || newRecipientBalance < 0)
                return new ValidationResult
                {
                    IsValid = false,
                    BlockIndex = block.Index,
                    Message = $"Transaction {trans.Id} results in a negative wallet balance."
                };

            balances[trans.From] = newSenderBalance;
            balances[trans.To] = newRecipientBalance;
        }
        return new ValidationResult
        {
            IsValid = true,
            BlockIndex = block.Index,
            Message = "Regular transactions are valid."
        };
    }
}