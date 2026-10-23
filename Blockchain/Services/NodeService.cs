using Blockchain.Models.Metrics;

namespace Blockchain.Services
{
    public class NodeService
    {
        private readonly BlockchainService _blockchainService;
        private readonly NetworkService _networkService;

        public NodeService(
            BlockchainService blockchainService,
            NetworkService networkService)
        {
            _blockchainService = blockchainService;
            _networkService = networkService;
        }

        public NodeStatistics GetNodeStatistics()
        {
            var chain = _blockchainService.Chain;
            var mempool = _blockchainService.MemPool;

            var minedBlocks = chain
                .Where(b => b.Index > 0)
                .ToList();

            return new NodeStatistics
            {
                Blocks = chain.Count,
                TransactionsInBlockchain = chain
                    .Sum(b => b.Transactions.Count),

                MempoolCount = mempool.Count,
                PendingFees = mempool
                    .Sum(t => t.Fee),
                AverageMiningTime = minedBlocks.Count > 0
                    ? minedBlocks.Average(b => b.MiningDuration)
                    : 0d,

                CurrentMiningReward = _blockchainService.GetMinerReward(chain.Count),
                NextHalvingBlock = _blockchainService.GetNextHalvingBlock(),
                PeersCount = _networkService.GetConnectedPeersCount()
            };
        }
    }
}
