using System;

namespace wacs.FLease.Messages
{
	public class Ballot
	{
		public DateTime Timestamp { get; set; }
		public int ProcessId { get; set; }
		public int MessageNumber { get; set; }
	}
}