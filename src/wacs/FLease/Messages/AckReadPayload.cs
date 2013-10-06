namespace wacs.FLease.Messages
{
	public class AckReadPayload : IMessagePayload
	{
		public Ballot Ballot { get; set; }
		public Ballot KnownWriteBallot { get; set; }
		public Lease Lease { get; set; }
	}
}