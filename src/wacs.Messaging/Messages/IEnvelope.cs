using wacs.Configuration;

namespace wacs.Messaging.Messages
{
	public interface IEnvelope
	{
		IProcess Sender { get; }
	}
}