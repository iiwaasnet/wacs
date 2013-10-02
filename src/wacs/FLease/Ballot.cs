using System;

namespace wacs.FLease
{
	public class Ballot : IBallot
	{
		private readonly DateTime timestamp;
		private readonly int messageNumber;
		private readonly IProcess process;

		public Ballot(DateTime timestamp, int messageNumber, IProcess process)
		{
			this.timestamp = timestamp;
			this.messageNumber = messageNumber;
			this.process = process;
		}

		public int Compare(IBallot x, IBallot y)
		{
			var X = (Ballot) x;
			var Y = (Ballot) y;

			var res = X.timestamp.CompareTo(Y.timestamp);
			if (res != 0)
			{
				return res;
			}

			res = X.messageNumber.CompareTo(Y.messageNumber);
			if (res != 0)
			{
				return res;
			}

			return X.process.Id.CompareTo(Y.process.Id);
		}
	}
}