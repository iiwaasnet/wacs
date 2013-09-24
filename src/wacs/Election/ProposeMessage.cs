using System.Threading;

namespace wacs.Election
{
	public class ProposeMessage
	{
		public static long Count = 0;

		public ProposeMessage()
		{
			Interlocked.Increment(ref Count);
		}

		public Candidate Candidate { get; set; }
	}
}