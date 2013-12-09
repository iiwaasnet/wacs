using System;

namespace wacs.Messaging
{
	public interface IListener : IObservable<IMessage>, IDisposable
	{
		void Start();

		void Stop();
	}
}