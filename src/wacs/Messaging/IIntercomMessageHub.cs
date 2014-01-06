using System;

namespace wacs.Messaging
{
	public interface IIntercomMessageHub : IDisposable
	{
		IListener Subscribe();

		void Broadcast(IMessage message);

		void Send(IProcess recipient, IMessage message);
	}
}