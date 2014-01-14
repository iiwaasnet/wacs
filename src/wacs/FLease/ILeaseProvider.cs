using System;
using System.Threading.Tasks;

namespace wacs.FLease
{
	public interface ILeaseProvider : IDisposable
	{
		void Start();
        void Stop();
		Task<ILease> GetLease();
	    void ResetLease();
	}
}