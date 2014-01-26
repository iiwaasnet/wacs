using System;
using System.Threading.Tasks;

namespace wacs.FLease
{
	public interface ILeaseProvider : IDisposable
	{
		ILease GetLease();
	    void ResetLease();
	}
}