namespace wacs.Messaging
{
    // TODO: Make destinct message class for each message type
	public class Message : IMessage
	{
		public IEnvelope Envelope { get; set; }
		public IBody Body { get; set; }
	}
}