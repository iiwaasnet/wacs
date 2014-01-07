using System;
using wacs.Configuration;
using wacs.Messaging.Messages;

namespace wacs.Messaging.Hubs.Intercom
{
	public interface IIntercomMessageHub : IDisposable
	{
		IListener Subscribe();

		void Broadcast(IMessage message);

		void Send(IProcess recipient, IMessage message);
	}
}