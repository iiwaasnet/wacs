namespace wacs.Messaging
{
	public interface IMessageHub
	{
		IListener Subscribe(IMessageSink messageSource);

		void Broadcast(IMessage message);

		void Send(IProcess process, IMessage message);
	}
}