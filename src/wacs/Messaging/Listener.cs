using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace wacs.Messaging
{
	public class Listener : IListener
	{
		private readonly ConcurrentDictionary<IObserver<IMessage>, object> observers;

		public Listener(IProcess subscriber)
		{
			Subscriber = subscriber;
			observers = new ConcurrentDictionary<IObserver<IMessage>, object>();
		}

		public void Notify(IMessage message)
		{
			foreach (var observer in observers)
			{
				observer.Key.OnNext(message);
			}
		}

		public IDisposable Subscribe(IObserver<IMessage> observer)
		{
			observers[observer] = null;

			return new Unsubscriber(observers, observer);
		}

		public void Start()
		{
		}

		public void Stop()
		{
			foreach (var observer in observers)
			{
				observer.Key.OnCompleted();
			}
		}

		public IProcess Subscriber { get; private set; }

		private class Unsubscriber : IDisposable
		{
			private readonly ConcurrentDictionary<IObserver<IMessage>, object> observers;
			private readonly IObserver<IMessage> observer;

			public Unsubscriber(ConcurrentDictionary<IObserver<IMessage>, object> observers, IObserver<IMessage> observer)
			{
				this.observer = observer;
				this.observers = observers;
			}

			public void Dispose()
			{
				if (observer != null)
				{
					object val;
					observers.TryRemove(observer, out val);
				}
			}
		}
	}
}