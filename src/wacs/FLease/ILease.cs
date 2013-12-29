using System;

namespace wacs.FLease
{
	public interface ILease
	{
		IProcess Owner { get; }

		DateTime ExpiresAt { get; }
	}
}