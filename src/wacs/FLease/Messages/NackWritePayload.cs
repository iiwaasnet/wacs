namespace wacs.FLease.Messages
{
	public class NackWritePayload : IMessagePayload
	{
		public Ballot Ballot { get; set; }
	}
}