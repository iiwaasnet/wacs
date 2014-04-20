using System;
using System.Collections.Generic;
using wacs.Communication.Hubs.Intercom;
using wacs.Messaging.Messages;

namespace tests.Unit.Helpers
{
    public class ListenerMock : IListener
    {
        private readonly IList<IObserver<IMessage>> observers;

        public ListenerMock()
        {
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

            return new Unsubscriber();
        }

        public void Dispose()
        {
        }

        public void Start()
        {
        }

        public void Stop()
        {
        }

        private class Unsubscriber : IDisposable
        {
            public void Dispose()
            {
            }
        }
    }
}