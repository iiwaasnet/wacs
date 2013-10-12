namespace wacs.FLease
{
	public interface ILeaseProviderFactory
	{
		ILeaseProvider Build(IProcess owner);
	}
}