namespace wacs.FLease
{
	public interface IRoundBasedRegister : ILeaseReader, ILeaseWriter
	{
		void Start();

		void Stop();
	}
}