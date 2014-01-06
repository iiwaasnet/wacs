using System.Collections.Concurrent;
using System.Threading;
using wacs.Diagnostics;

namespace wacs.Messaging.Inproc
{
    public class InprocIntercomMessageHub : IIntercomMessageHub
    {
        private readonly ConcurrentDictionary<Listener, object> subscriptions;
        private readonly BlockingCollection<ForwardRequest> p2p;
        private readonly BlockingCollection<BroadcastRequest> broadcast;
        private readonly ILogger logger;

        public InprocIntercomMessageHub(ILogger logger)
        {
            subscriptions = new ConcurrentDictionary<Listener, object>();
            p2p = new BlockingCollection<ForwardRequest>();
            broadcast = new BlockingCollection<BroadcastRequest>();
            this.logger = logger;

            new Thread(ForwardMessages).Start();
            new Thread(BroadcastMessages).Start();
        }

        public IListener Subscribe()
        {
            var listener = new Listener(Unsubscribe, logger);
            subscriptions.TryAdd(listener, null);

            return listener;
        }

        private void Unsubscribe(Listener listener)
        {
            object val;
            subscriptions.TryRemove(listener, out val);
        }

        public void Broadcast(IMessage message)
        {
            broadcast.Add(new BroadcastRequest {Message = message});
        }

        public void Send(IProcess recipient, IMessage message)
        {
            p2p.Add(new ForwardRequest {Recipient = recipient, Message = message});
        }

        private void BroadcastMessages()
        {
            foreach (var forwardRequest in broadcast.GetConsumingEnumerable())
            {
                foreach (var subscription in subscriptions.Keys)
                {
                    subscription.Notify(forwardRequest.Message);
                }
            }

            broadcast.Dispose();
        }

        private void ForwardMessages()
        {
            foreach (var forwardRequest in p2p.GetConsumingEnumerable())
            {
                foreach (var subscription in subscriptions.Keys)
                {
                    subscription.Notify(forwardRequest.Message);
                }
            }

            p2p.Dispose();
        }

        public void Dispose()
        {
            p2p.CompleteAdding();
            broadcast.CompleteAdding();
        }
    }
}