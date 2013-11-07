namespace wacs.Messaging
{
	public class Message : IMessage
	{
		public IEnvelope Envelope { get; set; }
		public IBody Body { get; set; }
	}
}