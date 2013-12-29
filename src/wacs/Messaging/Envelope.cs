namespace wacs.Messaging
{
	public class Envelope : IEnvelope
	{
		public IProcess Sender { get; set; }
	}
}