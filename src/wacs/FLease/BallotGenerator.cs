using System;

namespace wacs.FLease
{
	public class BallotGenerator : IBallotGenerator
	{
		private readonly TimeSpan maxLeaseTimeSpan;
		private readonly TimeSpan clockDrift;
		private readonly BallotTimestamp lastBallotTimestamp;

		public BallotGenerator(TimeSpan maxLeaseTimeSpan, TimeSpan clockDrift)
		{
			this.clockDrift = clockDrift;
			this.maxLeaseTimeSpan = maxLeaseTimeSpan;
			lastBallotTimestamp = new BallotTimestamp {MessageNumber = 0, Timestamp = DateTime.UtcNow};
		}

		public IBallot New(IProcess owner)
		{
			var now = DateTime.UtcNow;
			lastBallotTimestamp.MessageNumber = (lastBallotTimestamp.Timestamp == now)
				                                    ? lastBallotTimestamp.MessageNumber++
				                                    : 0;
			lastBallotTimestamp.Timestamp = now;

			return new Ballot(lastBallotTimestamp.Timestamp, lastBallotTimestamp.MessageNumber, owner);
		}

		public IBallot Null()
		{
			return null;
		}
	}

	internal class BallotTimestamp
	{
		internal DateTime Timestamp { get; set; }

		internal int MessageNumber { get; set; }
	}
}