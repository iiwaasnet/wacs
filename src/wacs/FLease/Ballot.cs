using System;

namespace wacs.FLease
{
	public class Ballot : IBallot
	{
		public Ballot(DateTime timestamp, int messageNumber, IProcess process)
		{
			Timestamp = timestamp;
			MessageNumber = messageNumber;
			Process = process;
		}

		public int Compare(IBallot x, IBallot y)
		{
			var res = x.Timestamp.CompareTo(y.Timestamp);
			if (res != 0)
			{
				return res;
			}

			res = x.MessageNumber.CompareTo(y.MessageNumber);
			if (res != 0)
			{
				return res;
			}

			return x.Process.Id.CompareTo(y.Process.Id);
		}

		public IProcess Process { get; private set; }
		public DateTime Timestamp { get; private set; }
		public int MessageNumber { get; private set; }
	}
}