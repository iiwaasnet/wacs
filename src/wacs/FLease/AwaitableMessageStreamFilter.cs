using System;
using System.Collections.Generic;
using System.Threading;
using wacs.Configuration;
using wacs.Messaging.Messages;

namespace wacs.FLease
{
    public class AwaitableMessageStreamFilter : IObserver<IMessage>, IDisposable
    {
        private readonly Func<IMessage, bool> predicate;
        private readonly int maxCount;
        private int currentCount;
        private readonly ManualResetEventSlim awaitable;
        private readonly IDictionary<IProcess, IMessage> messages;
        private readonly object locker = new object();

        public AwaitableMessageStreamFilter(Func<IMessage, bool> predicate, int maxCount)
        {
            this.predicate = predicate;
            this.maxCount = maxCount;
            currentCount = 0;
            messages = new Dictionary<IProcess, IMessage>();
            awaitable = new ManualResetEventSlim(false);
        }

        public void OnNext(IMessage value)
        {
            if (predicate(value))
            {
                lock (locker)
                {
                    var process = new Process(value.Envelope.Sender.Id);

                    if (!awaitable.IsSet)
                    {
                        if (!messages.ContainsKey(process))
                        {
                            messages[process] = value;
                            currentCount++;
                        }
                    }
                    if (currentCount == maxCount && !awaitable.IsSet)
                    {
                        awaitable.Set();
                    }
                }
            }
        }

        public void OnError(Exception error)
        {
        }

        public void OnCompleted()
        {
        }

        public void Dispose()
        {
            awaitable.Dispose();
        }

        public WaitHandle Filtered
        {
            get { return awaitable.WaitHandle; }
        }

        public IEnumerable<IMessage> MessageStream
        {
            get
            {
                awaitable.Wait();

                return messages.Values;
            }
        }
    }
}