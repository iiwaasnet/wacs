namespace wacs.FLease.Messages
{
	public class AckReadMessage
	{
		public Ballot Ballot { get; set; }
		public Ballot KnownWriteBallot { get; set; }
		public Lease Lease { get; set; }
	}
}