namespace wacs.Messaging
{
	public interface IMessageHub
	{
		IListener Subscribe(IProcess subscriber);

		void Broadcast(IMessage message);

		void Send(IProcess recipient, IMessage message);
	}
}