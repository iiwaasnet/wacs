namespace wacs.Messaging.Messages
{
	public class Body : IBody
	{
		public string MessageType { get; set; }
		public byte[] Content { get; set; }
	}
}