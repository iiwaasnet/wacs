namespace wacs.FLease
{
	public interface IRoundBasedRegister : ILeaseReader, ILeaseWriter
	{
		void SetOwner(IProcess process);

		void Start();

		void Stop();
	}
}