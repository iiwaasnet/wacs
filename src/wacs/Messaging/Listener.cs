using System;
using System.Collections.Generic;

namespace wacs.Messaging
{
	public class Listener : IListener
	{
		private readonly IList<IObserver<IMessage>> observers;

		public Listener(IProcess subscriber)
		{
			Subscriber = subscriber;
			observers = new List<IObserver<IMessage>>();
		}

		public void Notify(IMessage message)
		{
			foreach (var observer in observers)
			{
				observer.OnNext(message);
			}
		}

		public IDisposable Subscribe(IObserver<IMessage> observer)
		{
			observers.Add(observer);

			return new Unsubscriber(observers, observer);
		}

		public void Start()
		{
		}

		public void Stop()
		{
			foreach (var observer in observers)
			{
				observer.OnCompleted();
			}
		}

		public IProcess Subscriber { get; private set; }

		private class Unsubscriber : IDisposable
		{
			private readonly IList<IObserver<IMessage>> observers;
			private readonly IObserver<IMessage> observer;

			public Unsubscriber(IList<IObserver<IMessage>> observers, IObserver<IMessage> observer)
			{
				this.observer = observer;
				this.observers = observers;
			}

			public void Dispose()
			{
				if (observer != null && observers.Contains(observer))
				{
					observers.Remove(observer);
				}
			}
		}
	}
}