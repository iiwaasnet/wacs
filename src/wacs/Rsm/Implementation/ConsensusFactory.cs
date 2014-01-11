using wacs.Configuration;
using wacs.Messaging.Hubs.Intercom;
using wacs.Resolver;
using wacs.Rsm.Interface;

namespace wacs.Rsm.Implementation
{
    public class ConsensusFactory : IConsensusFactory
    {
        private readonly IConsensusRoundManager consensusRoundManager;
        private readonly IIntercomMessageHub intercomMessageHub;
        private readonly ISynodConfigurationProvider synodConfigurationProvider;
        private readonly INodeResolver nodeResolver;

        public ConsensusFactory(IConsensusRoundManager consensusRoundManager,
                                IIntercomMessageHub intercomMessageHub,
                                ISynodConfigurationProvider synodConfigurationProvider,
                                INodeResolver nodeResolver)
        {
            this.consensusRoundManager = consensusRoundManager;
            this.intercomMessageHub = intercomMessageHub;
            this.synodConfigurationProvider = synodConfigurationProvider;
            this.nodeResolver = nodeResolver;
        }

        public IConsensus CreateInstance()
        {
            return new Consensus(consensusRoundManager, intercomMessageHub, synodConfigurationProvider, nodeResolver);
        }
    }
}