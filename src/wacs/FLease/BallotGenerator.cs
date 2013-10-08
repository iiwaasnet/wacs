using System;

namespace wacs.FLease
{
	public class BallotGenerator : IBallotGenerator
	{
		private readonly BallotTimestamp lastBallotTimestamp;
		private readonly IFleaseConfiguration config;

		public BallotGenerator(IFleaseConfiguration config)
		{
			this.config = config;
			lastBallotTimestamp = new BallotTimestamp {MessageNumber = 0, Timestamp = DateTime.UtcNow};
		}

		public IBallot New(IProcess owner)
		{
			var now = DateTime.UtcNow;
			lastBallotTimestamp.MessageNumber = (lastBallotTimestamp.Timestamp + config.ClockDrift == now)
				                                    ? lastBallotTimestamp.MessageNumber++
				                                    : 0;
			lastBallotTimestamp.Timestamp = now;

			return new Ballot(lastBallotTimestamp.Timestamp, lastBallotTimestamp.MessageNumber, owner);
		}

		public IBallot Null()
		{
			return new Ballot(DateTime.MinValue, 0, new Process(0));
		}
	}

	internal class BallotTimestamp
	{
		internal DateTime Timestamp { get; set; }

		internal int MessageNumber { get; set; }
	}
}