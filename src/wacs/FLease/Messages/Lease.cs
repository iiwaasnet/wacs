using System;

namespace wacs.FLease.Messages
{
	public class Lease
	{
		public int ProcessId { get; set; }
		public DateTime ExpiresAt { get; set; }
	}
}