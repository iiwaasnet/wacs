namespace wacs.FLease
{
	public interface ILeaseTxResult
	{
		TxOutcome TxOutcome { get; }
		ILease Lease { get; }
	}
}