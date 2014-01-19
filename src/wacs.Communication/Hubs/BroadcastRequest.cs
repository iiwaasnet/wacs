using wacs.Messaging.Messages;

namespace wacs.Communication.Hubs
{
	public class BroadcastRequest
	{
		public IMessage Message { get; set; }
	}
}