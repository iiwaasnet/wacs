namespace wacs.FLease.Messages
{
	public class WritePayload : IMessagePayload
	{
		public Ballot Ballot { get; set; }

		public Lease Lease { get; set; }
	}
}