namespace wacs.Messaging
{
	public class Envelope : IEnvelope
	{
		public ISender Sender { get; set; }
	}
}