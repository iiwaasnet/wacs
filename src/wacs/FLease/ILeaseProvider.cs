using System;
using System.Threading.Tasks;

namespace wacs.FLease
{
	public interface ILeaseProvider : IDisposable
	{
		void Start();

		Task<ILease> GetLease();

		void Stop();
	}
}