using System;

namespace wacs.FLease
{
	public class Ballot : IBallot
	{
		public Ballot(DateTime timestamp, int messageNumber, INode node)
		{
			Timestamp = timestamp;
			MessageNumber = messageNumber;
			Node = node;
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

			return Node.Id.CompareTo(ballot.Node.Id);
		}

		public INode Node { get; private set; }
		public DateTime Timestamp { get; private set; }
		public int MessageNumber { get; private set; }
	}
}