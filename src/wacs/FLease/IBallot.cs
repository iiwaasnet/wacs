using System;
using wacs.Configuration;
using wacs.Messaging;
using wacs.Messaging.Messages;

namespace wacs.FLease
{
	public interface IBallot : IComparable
	{
		IProcess Process { get; }
		DateTime Timestamp { get; }
		int MessageNumber { get; }
	}
}