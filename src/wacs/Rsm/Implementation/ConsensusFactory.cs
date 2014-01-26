using wacs.Communication.Hubs.Intercom;
using wacs.Configuration;
using wacs.Diagnostics;
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
        private readonly IRsmConfiguration rsmConfig;
        private readonly IReplicatedLog replicatedLog;
        private readonly ILogger logger;

        public ConsensusFactory(IConsensusRoundManager consensusRoundManager,
                                IIntercomMessageHub intercomMessageHub,
                                ISynodConfigurationProvider synodConfigurationProvider,
                                IReplicatedLog replicatedLog,
                                INodeResolver nodeResolver,
                                IRsmConfiguration rsmConfig,
                                ILogger logger)
        {
            this.consensusRoundManager = consensusRoundManager;
            this.intercomMessageHub = intercomMessageHub;
            this.synodConfigurationProvider = synodConfigurationProvider;
            this.nodeResolver = nodeResolver;
            this.rsmConfig = rsmConfig;
            this.replicatedLog = replicatedLog;
            this.logger = logger;
        }

        public IConsensus CreateInstance()
        {
            return new Consensus(consensusRoundManager,
                                 intercomMessageHub,
                                 synodConfigurationProvider,
                                 replicatedLog,
                                 nodeResolver,
                                 rsmConfig,
                                 logger);
        }
    }
}