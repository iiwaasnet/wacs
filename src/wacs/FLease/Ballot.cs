using System;
using System.Diagnostics.Contracts;

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

		//public bool LessThan(IBallot ballot)
		//{
		//	return CompareTo(ballot) < 0;
		//}

		//public bool GreaterThan(IBallot ballot)
		//{
		//	return CompareTo(ballot) > 0;
		//}

		//public bool Equals(IBallot ballot)
		//{
		//	return CompareTo(ballot) < 0;
		//}

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

		public int CompareTo(object obj)
		{
			var ballot = obj as Ballot;
			Contract.Requires(ballot != null);

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

			return Process.Id.CompareTo(ballot.Process.Id);
		}

		public IProcess Process { get; private set; }
		public DateTime Timestamp { get; private set; }
		public int MessageNumber { get; private set; }
	}
}