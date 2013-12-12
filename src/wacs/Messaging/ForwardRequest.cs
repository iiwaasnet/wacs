namespace wacs.Messaging
{
	public class ForwardRequest
	{
		public INode Recipient { get; set; }

		public IMessage Message { get; set; }
	}
}