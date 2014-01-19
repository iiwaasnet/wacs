using wacs.Configuration;
using wacs.Messaging.Messages;

namespace wacs.Communication.Hubs.Intercom
{
	public class ForwardRequest
	{
		public IProcess Recipient { get; set; }

		public IMessage Message { get; set; }
	}
}