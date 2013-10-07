using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

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
			using (var gateway = new AutoResetEvent(false))
			{
				foreach (var observer in observers)
				{
					Task.Factory.StartNew(() =>
						                      {
												  gateway.Set();
							                      observer.Key.OnNext(message);
						                      });
					gateway.WaitOne();
				}
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