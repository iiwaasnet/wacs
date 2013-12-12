using System;

namespace wacs.FLease
{
	public class LastWrittenLease : IComparable<LastWrittenLease>
	{
		private readonly Ballot writeBallot;
		private readonly Lease lease;

		public LastWrittenLease(Messages.Ballot writeBallot, Messages.Lease lease)
		{
			this.writeBallot = new Ballot(new DateTime(writeBallot.Timestamp, DateTimeKind.Utc),
			                              writeBallot.MessageNumber,
			                              new Node(writeBallot.ProcessId));
			this.lease = (lease != null)
				             ? new Lease(new Node(lease.ProcessId), new DateTime(lease.ExpiresAt, DateTimeKind.Utc))
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