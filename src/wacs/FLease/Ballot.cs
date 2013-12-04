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

		public static bool operator <=(Ballot x, Ballot y)
		{
			var res = x.CompareTo(y);

			return res < 0 || res == 0;
		}

		public static bool operator >=(Ballot x, Ballot y)
		{
			var res = x.CompareTo(y);

			return res > 0 || res == 0;
		}

		public static bool operator <(Ballot x, Ballot y)
		{
			return x.CompareTo(y) < 0;
		}

		public static bool operator >(Ballot x, Ballot y)
		{
			return x.CompareTo(y) > 0;
		}

		public int CompareTo(object obj)
		{
			var ballot = obj as Ballot;

			var res = Timestamp.CompareTo(ballot.Timestamp);
			if (res != 0)
			{
				return res;
			}

			res = MessageNumber.CompareTo(ballot.MessageNumber);
			if (res != 0)
			{
				return res;
			}

			return Process.Name.CompareTo(ballot.Process.Name);
		}

		public IProcess Process { get; private set; }
		public DateTime Timestamp { get; private set; }
		public int MessageNumber { get; private set; }
	}
}