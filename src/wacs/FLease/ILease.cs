using System;
using wacs.Configuration;
using wacs.Messaging;
using wacs.Messaging.Messages;

namespace wacs.FLease
{
	public interface ILease
	{
		IProcess Owner { get; }

		DateTime ExpiresAt { get; }
	}
}