namespace wacs.FLease
{
	public interface ILeaseReader
	{
		ILeaseTxResult Read(IBallot ballot);
	}
}