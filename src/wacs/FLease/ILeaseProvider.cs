using System;
using System.Threading.Tasks;

namespace wacs.FLease
{
	public interface ILeaseProvider : IDisposable
	{
		Task<ILease> GetLease();
	    void ResetLease();
	}
}