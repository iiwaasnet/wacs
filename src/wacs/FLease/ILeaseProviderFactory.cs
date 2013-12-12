namespace wacs.FLease
{
	public interface ILeaseProviderFactory
	{
		ILeaseProvider Build(INode owner);
	}
}