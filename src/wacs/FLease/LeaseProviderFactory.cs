using wacs.Configuration;
using wacs.Diagnostics;

namespace wacs.FLease
{
	public class LeaseProviderFactory : ILeaseProviderFactory
	{
		private readonly IRoundBasedRegisterFactory registerFactory;
		private readonly IBallotGenerator ballotGenerator;
		private readonly ILeaseConfiguration config;
		private readonly ILogger logger;

		public LeaseProviderFactory(IRoundBasedRegisterFactory registerFactory,
		                            IBallotGenerator ballotGenerator,
		                            ILeaseConfiguration config,
		                            ILogger logger)
		{
			this.registerFactory = registerFactory;
			this.config = config;
			this.ballotGenerator = ballotGenerator;
			this.logger = logger;
		}

		public ILeaseProvider Build(INode owner)
		{
			return new LeaseProvider(owner, registerFactory, ballotGenerator, config, logger);
		}
	}
}