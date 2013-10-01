namespace wacs.FLease
{
	public interface ILeaseProvider
	{
		ILease GetLease(IBallot ballot);
	}
}