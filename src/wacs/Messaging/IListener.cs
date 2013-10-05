using System;

namespace wacs.Messaging
{
	public interface IListener : IObservable<IMessage>
	{
		void Start();

		void Stop();
	}
}