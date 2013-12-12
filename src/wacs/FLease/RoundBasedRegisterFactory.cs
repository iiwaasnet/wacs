using wacs.Configuration;
using wacs.Diagnostics;
using wacs.Messaging;

namespace wacs.FLease
{
    public class RoundBasedRegisterFactory : IRoundBasedRegisterFactory
    {
        private readonly IMessageHub messageHub;
        private readonly IBallotGenerator ballotGenerator;
        private readonly IMessageSerializer serializer;
        private readonly ISynodConfiguration synodConfig;
        private readonly ILeaseConfiguration leaseConfig;
        private readonly ILogger logger;

        public RoundBasedRegisterFactory(IMessageHub messageHub,
                                         IBallotGenerator ballotGenerator,
                                         IMessageSerializer serializer,
                                         ISynodConfiguration synodConfig,
                                         ILeaseConfiguration leaseConfig,
                                         ILogger logger)
        {
            this.messageHub = messageHub;
            this.ballotGenerator = ballotGenerator;
            this.serializer = serializer;
            this.synodConfig = synodConfig;
            this.leaseConfig = leaseConfig;
            this.logger = logger;
        }

        public IRoundBasedRegister Build(INode owner)
        {
            return new RoundBasedRegister(owner, messageHub, ballotGenerator, serializer, synodConfig, leaseConfig, logger);
        }
    }
}