using wacs.Configuration;
using wacs.Diagnostics;
using wacs.Messaging;
using wacs.Resolver.Interface;

namespace wacs.FLease
{
    public class RoundBasedRegisterFactory : IRoundBasedRegisterFactory
    {
        private readonly IMessageHub messageHub;
        private readonly IBallotGenerator ballotGenerator;
        private readonly ITopologyConfiguration topology;
        private readonly ILeaseConfiguration leaseConfig;
        private readonly INodeResolver nodeResolver;
        private readonly ILogger logger;

        public RoundBasedRegisterFactory(IMessageHub messageHub,
                                         IBallotGenerator ballotGenerator,
                                         ITopologyConfiguration topology,
                                         ILeaseConfiguration leaseConfig,
                                         INodeResolver nodeResolver,
                                         ILogger logger)
        {
            this.messageHub = messageHub;
            this.ballotGenerator = ballotGenerator;
            this.topology = topology;
            this.leaseConfig = leaseConfig;
            this.logger = logger;
            this.nodeResolver = nodeResolver;
        }

        public IRoundBasedRegister Build(IProcess owner)
        {
            return new RoundBasedRegister(owner, messageHub, ballotGenerator, topology, leaseConfig, nodeResolver, logger);
        }
    }
}