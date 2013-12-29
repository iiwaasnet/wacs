using System;

namespace wacs.FLease
{
	public interface IBallot : IComparable
	{
		IProcess Process { get; }
		DateTime Timestamp { get; }
		int MessageNumber { get; }
	}
}