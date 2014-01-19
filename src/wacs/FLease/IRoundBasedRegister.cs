using System;

namespace wacs.FLease
{
	public interface IRoundBasedRegister : ILeaseReader, ILeaseWriter, IDisposable
	{
	}
}