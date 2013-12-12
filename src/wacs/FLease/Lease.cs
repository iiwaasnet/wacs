using System;

namespace wacs.FLease
{
	public class Lease : ILease
	{
		public Lease(INode owner, DateTime expiresAt)
		{
			Owner = owner;
			ExpiresAt = expiresAt;
		}

		public INode Owner { get; private set; }
		public DateTime ExpiresAt { get; private set; }
	}
}