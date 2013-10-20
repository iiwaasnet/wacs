using System;

namespace wacs.FLease.Messages
{
	public class Ballot
	{
		public long Timestamp { get; set; }
		public string ProcessId { get; set; }
		public int MessageNumber { get; set; }
	}
}