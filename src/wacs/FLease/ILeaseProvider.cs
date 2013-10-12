using System.Threading.Tasks;

namespace wacs.FLease
{
	public interface ILeaseProvider
	{
		void Start();

		Task<ILease> GetLease();

		void Stop();
	}
}