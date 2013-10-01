namespace wacs.Messaging
{
	public interface IEnvelope
	{
		ISender Sender { get; }
	}
}