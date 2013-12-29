namespace wacs.Messaging
{
	public interface IEnvelope
	{
		IProcess Sender { get; }
	}
}