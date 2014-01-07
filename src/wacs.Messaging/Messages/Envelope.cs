using wacs.Configuration;

namespace wacs.Messaging.Messages
{
	public class Envelope : IEnvelope
	{
		public IProcess Sender { get; set; }
	}
}