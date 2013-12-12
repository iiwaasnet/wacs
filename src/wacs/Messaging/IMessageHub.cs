using System;

namespace wacs.Messaging
{
	public interface IMessageHub : IDisposable
	{
		IListener Subscribe(INode subscriber);

		void Broadcast(IMessage message);

		void Send(INode recipient, IMessage message);
	}
}