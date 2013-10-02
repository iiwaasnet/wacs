namespace wacs.FLease
{
	public class RoundBasedRegister : IRoundBasedRegister
	{
		private IProcess owner;
		private IBallot readBallot;
		private IBallot writeBallot;
		private ILease lease;

		public RoundBasedRegister(IBallotGenerator ballotGenerator)
		{
			readBallot = ballotGenerator.Null();
			writeBallot = ballotGenerator.Null();
		}

		public void SetOwner(IProcess process)
		{
			this.owner = owner;
		}

		public ILeaseTxResult Read(IBallot ballot)
		{
			throw new System.NotImplementedException();
		}

		public ILeaseTxResult Write(IBallot ballot, ILease lease)
		{
			throw new System.NotImplementedException();
		}
	}
}