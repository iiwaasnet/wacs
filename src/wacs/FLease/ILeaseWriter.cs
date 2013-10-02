namespace wacs.FLease
{
	public interface ILeaseWriter
	{
		ILeaseTxResult Write(IBallot ballot, ILease lease);
	}
}