namespace wacs.FLease
{
	public partial class RoundBasedRegister
	{
		private void LogNackRead(Ballot ballot)
		{
			if (writeBallot >= ballot)
			{
				logger.DebugFormat("Node {6} ==WB== {0}-{1}-{2} >= {3}-{4}-{5}",
				                   writeBallot.Timestamp.ToString("HH:mm:ss fff"),
				                   writeBallot.MessageNumber,
				                   writeBallot.Node.Id,
				                   ballot.Timestamp.ToString("HH:mm:ss fff"),
				                   ballot.MessageNumber,
				                   ballot.Node.Id,
				                   owner.Id);
			}
			if (readBallot >= ballot)
			{
				logger.DebugFormat("Node {6} ==RB== {0}-{1}-{2} >= {3}-{4}-{5}",
				                   readBallot.Timestamp.ToString("HH:mm:ss fff"),
				                   readBallot.MessageNumber,
				                   readBallot.Node.Id,
				                   ballot.Timestamp.ToString("HH:mm:ss fff"),
				                   ballot.MessageNumber,
				                   ballot.Node.Id,
				                   owner.Id);
			}
		}
	}
}