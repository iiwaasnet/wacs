using System;
using wacs.Configuration;
using wacs.Messaging;
using wacs.Messaging.Messages;

namespace wacs.FLease
{
	public class BallotGenerator : IBallotGenerator
	{
		private readonly BallotTimestamp lastBallotTimestamp;
		private readonly ILeaseConfiguration config;

		public BallotGenerator(ILeaseConfiguration config)
		{
			this.config = config;
			lastBallotTimestamp = new BallotTimestamp {MessageNumber = 0, Timestamp = DateTime.UtcNow};
		}

		public IBallot New(IProcess owner)
		{
			var now = DateTime.UtcNow;

			if (now >= lastBallotTimestamp.Timestamp
			    || now <= lastBallotTimestamp.Timestamp + config.ClockDrift)
			{
				lastBallotTimestamp.MessageNumber = ++lastBallotTimestamp.MessageNumber;
			}
			else
			{
				lastBallotTimestamp.MessageNumber = 0;
			}

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