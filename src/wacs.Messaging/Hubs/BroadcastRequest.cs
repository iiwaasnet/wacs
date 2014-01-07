using wacs.Messaging.Messages;

namespace wacs.Messaging.Hubs
{
	public class BroadcastRequest
	{
		public IMessage Message { get; set; }
	}
}