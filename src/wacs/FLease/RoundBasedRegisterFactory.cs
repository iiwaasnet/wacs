using wacs.Diagnostics;
using wacs.Messaging;

namespace wacs.FLease
{
	public class RoundBasedRegisterFactory : IRoundBasedRegisterFactory
	{
		private readonly IMessageHub messageHub;
		private readonly IBallotGenerator ballotGenerator;
		private readonly IMessageSerializer serializer;
		private readonly IWacsConfiguration config;
		private readonly ILogger logger;

		public RoundBasedRegisterFactory(IMessageHub messageHub,
		                                 IBallotGenerator ballotGenerator,
		                                 IMessageSerializer serializer,
		                                 IWacsConfiguration config,
		                                 ILogger logger)
		{
			this.messageHub = messageHub;
			this.ballotGenerator = ballotGenerator;
			this.serializer = serializer;
			this.config = config;
			this.logger = logger;
		}

		public IRoundBasedRegister Build(IProcess owner)
		{
			return new RoundBasedRegister(owner, messageHub, ballotGenerator, serializer, config, logger);
		}
	}
}