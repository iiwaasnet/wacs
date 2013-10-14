using wacs.Diagnostics;

namespace wacs.FLease
{
	public class LeaseProviderFactory : ILeaseProviderFactory
	{
		private readonly IRoundBasedRegisterFactory registerFactory;
		private readonly IBallotGenerator ballotGenerator;
		private readonly IFleaseConfiguration config;
		private readonly ILogger logger;

		public LeaseProviderFactory(IRoundBasedRegisterFactory registerFactory,
		                            IBallotGenerator ballotGenerator,
		                            IFleaseConfiguration config,
		                            ILogger logger)
		{
			this.registerFactory = registerFactory;
			this.config = config;
			this.ballotGenerator = ballotGenerator;
			this.logger = logger;
		}

		public ILeaseProvider Build(IProcess owner)
		{
			return new LeaseProvider(owner, registerFactory, ballotGenerator, config, logger);
		}
	}
}