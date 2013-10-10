using System.Threading.Tasks;

namespace wacs.FLease
{
	public interface ILeaseProvider
	{
		void Start(IProcess owner);

		Task<ILease> GetLease();

		void Stop();
	}
}