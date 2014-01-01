using System;
using System.Collections.Concurrent;
using System.Threading;
using wacs.Diagnostics;

namespace wacs.Messaging
{
    public class Listener : IListener
    {
        private readonly ConcurrentDictionary<IObserver<IMessage>, object> observers;
        private readonly BlockingCollection<IMessage> messages;
        private readonly CancellationTokenSource cancellationSource;
        private Action<IMessage> appendMessage;
        private readonly Action<Listener> unsubscribe;
        private readonly ILogger logger;

        public Listener(Action<Listener> unsubscribe, ILogger logger)
        {
            observers = new ConcurrentDictionary<IObserver<IMessage>, object>();
            messages = new BlockingCollection<IMessage>(new ConcurrentQueue<IMessage>());
            appendMessage = DropMessage;
            this.unsubscribe = unsubscribe;
            cancellationSource = new CancellationTokenSource();
            this.logger = logger;
            new Thread(() => ForwardMessages(cancellationSource.Token)).Start();
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

        private void ForwardMessages(CancellationToken token)
        {
            foreach (var message in messages.GetConsumingEnumerable(token))
            {
                foreach (var observer in observers)
                {
                    try
                    {
                        observer.Key.OnNext(message);
                    }
                    catch (Exception err)
                    {
                        logger.Error(err);
                    }
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
            unsubscribe(this);
            Stop();
            cancellationSource.Cancel(false);
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