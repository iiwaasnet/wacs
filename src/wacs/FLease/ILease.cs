using System;

namespace wacs.FLease
{
	public interface ILease
	{
		INode Owner { get; }

		DateTime ExpiresAt { get; }
	}
}