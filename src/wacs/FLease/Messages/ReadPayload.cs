namespace wacs.FLease.Messages
{
	public class ReadPayload : IMessagePayload
	{
		public Ballot Ballot { get; set; }
	}
}