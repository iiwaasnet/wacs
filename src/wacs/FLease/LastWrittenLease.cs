using System;
using wacs.Configuration;

namespace wacs.FLease
{
	public class LastWrittenLease : IComparable<LastWrittenLease>
	{
		private readonly Ballot writeBallot;
		private readonly Lease lease;

		public LastWrittenLease(Messaging.Messages.Intercom.Lease.Ballot writeBallot, Messaging.Messages.Intercom.Lease.Lease lease)
		{
			this.writeBallot = new Ballot(new DateTime(writeBallot.Timestamp, DateTimeKind.Utc),
			                              writeBallot.MessageNumber,
			                              new Process(writeBallot.ProcessId));
			this.lease = (lease != null)
				             ? new Lease(new Process(lease.ProcessId), new DateTime(lease.ExpiresAt, DateTimeKind.Utc))
				             : null;
		}

		public int CompareTo(LastWrittenLease other)
		{
			return writeBallot.CompareTo(other.writeBallot);
		}

		public Lease Lease
		{
			get { return lease; }
		}
	}
}