using System.Threading;

namespace wacs.Election
{
	public class AcceptMessage
	{
		public static long Count = 0;

		public AcceptMessage()
		{
			Interlocked.Increment(ref Count);
		}

		public Candidate Candidate { get; set; }

		public Candidate Elector { get; set; }
	}
}