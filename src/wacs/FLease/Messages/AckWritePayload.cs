namespace wacs.FLease.Messages
{
	public class AckWritePayload : IMessagePayload
	{
		public Ballot Ballot { get; set; }
	}
}