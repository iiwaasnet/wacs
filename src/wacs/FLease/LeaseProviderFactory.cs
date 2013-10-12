namespace wacs.FLease
{
	public class LeaseProviderFactory : ILeaseProviderFactory
	{
		private readonly IRoundBasedRegisterFactory registerFactory;
		private readonly IBallotGenerator ballotGenerator;
		private readonly IFleaseConfiguration config;

		public LeaseProviderFactory(IRoundBasedRegisterFactory registerFactory,
		                            IBallotGenerator ballotGenerator,
		                            IFleaseConfiguration config)
		{
			this.registerFactory = registerFactory;
			this.config = config;
			this.ballotGenerator = ballotGenerator;
		}

		public ILeaseProvider Build(IProcess owner)
		{
			return new LeaseProvider(owner, registerFactory, ballotGenerator, config);
		}
	}
}