namespace wacs.FLease
{
	public partial class RoundBasedRegister
	{
		private void LogNackRead(Ballot ballot)
		{
			if (writeBallot >= ballot)
			{
				logger.DebugFormat("Process {6} ==WB== {0}-{1}-{2} >= {3}-{4}-{5}",
				                   writeBallot.Timestamp.ToString("HH:mm:ss fff"),
				                   writeBallot.MessageNumber,
				                   writeBallot.Process.Id,
				                   ballot.Timestamp.ToString("HH:mm:ss fff"),
				                   ballot.MessageNumber,
				                   ballot.Process.Id,
				                   owner.Id);
			}
			if (readBallot >= ballot)
			{
				logger.DebugFormat("Process {6} ==RB== {0}-{1}-{2} >= {3}-{4}-{5}",
				                   readBallot.Timestamp.ToString("HH:mm:ss fff"),
				                   readBallot.MessageNumber,
				                   readBallot.Process.Id,
				                   ballot.Timestamp.ToString("HH:mm:ss fff"),
				                   ballot.MessageNumber,
				                   ballot.Process.Id,
				                   owner.Id);
			}
		}
	}
}