using System;
using wacs.Messaging.Messages;

namespace wacs.Messaging.Hubs.Intercom
{
	public interface IListener : IObservable<IMessage>, IDisposable
	{
		void Start();

		void Stop();
	}
}