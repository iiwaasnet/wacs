using System;
using System.Collections.Concurrent;
using System.Threading;

namespace wacs.Messaging
{
    public class Listener : IListener
    {
        private readonly ConcurrentDictionary<IObserver<IMessage>, object> observers;
        private readonly BlockingCollection<IMessage> messages;
        private readonly Thread notifyThread;
        private Action<IMessage> appendMessage;

        public Listener()
        {
            observers = new ConcurrentDictionary<IObserver<IMessage>, object>();
            messages = new BlockingCollection<IMessage>(new ConcurrentQueue<IMessage>());
            appendMessage = DropMessage;
            notifyThread = new Thread(ForwardMessages);
            notifyThread.Start();
        }

        public void Notify(IMessage message)
        {
            appendMessage(message);
        }

        private void AddMessageToQueue(IMessage message)
        {
            messages.Add(message);
        }

        private void DropMessage(IMessage message)
        {
        }

        private void ForwardMessages()
        {
            foreach (var message in messages.GetConsumingEnumerable())
            {
                foreach (var observer in observers)
                {
                    observer.Key.OnNext(message);
                }
            }
            messages.Dispose();
        }

        public IDisposable Subscribe(IObserver<IMessage> observer)
        {
            observers[observer] = null;

            return new Unsubscriber(observers, observer);
        }

        public void Start()
        {
            Interlocked.Exchange(ref appendMessage, AddMessageToQueue);
        }

        public void Stop()
        {
            Interlocked.Exchange(ref appendMessage, DropMessage);
        }

        public void Dispose()
        {
            Stop();
            messages.CompleteAdding();
        }

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