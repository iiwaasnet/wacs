using System;
using wacs.Messaging.Messages;

namespace wacs.Communication.Hubs.Intercom
{
	public interface IListener : IObservable<IMessage>, IDisposable
	{
		void Start();

		void Stop();
	}
}