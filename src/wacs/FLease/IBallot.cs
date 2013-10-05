using System;
using System.Collections.Generic;

namespace wacs.FLease
{
	public interface IBallot : IComparer<IBallot>
	{
		IProcess Process { get; }
		DateTime Timestamp { get; }
		int MessageNumber { get; }
	}
}