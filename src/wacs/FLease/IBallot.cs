using System;

namespace wacs.FLease
{
	public interface IBallot : IComparable
	{
		INode Node { get; }
		DateTime Timestamp { get; }
		int MessageNumber { get; }
	}
}