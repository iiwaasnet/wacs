namespace wacs.Messaging
{
	public interface IEnvelope
	{
		INode Sender { get; }
	}
}