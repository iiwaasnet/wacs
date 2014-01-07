using System;
using wacs.Configuration;
using wacs.Messaging;
using wacs.Messaging.Messages;

namespace wacs.FLease
{
	public class Lease : ILease
	{
		public Lease(IProcess owner, DateTime expiresAt)
		{
			Owner = owner;
			ExpiresAt = expiresAt;
		}

		public IProcess Owner { get; private set; }
		public DateTime ExpiresAt { get; private set; }
	}
}