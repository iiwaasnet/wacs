namespace wacs.FLease.Messages
{
	public class NackReadPayload : IMessagePayload
	{
		public Ballot Ballot { get; set; }
	}
}