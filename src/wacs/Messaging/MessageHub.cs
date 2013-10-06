using System.Collections.Concurrent;
using System.Linq;
using System.Threading;

namespace wacs.Messaging
{
	public class MessageHub : IMessageHub
	{
		private readonly ConcurrentBag<Listener> subscriptions;
		private readonly BlockingCollection<ForwardRequest> p2p;
		private readonly BlockingCollection<BroadcastRequest> broadcast;

		public MessageHub()
		{
			subscriptions = new ConcurrentBag<Listener>();
			p2p = new BlockingCollection<ForwardRequest>();
			broadcast = new BlockingCollection<BroadcastRequest>();

			new Thread(ForwardMessages).Start();
			new Thread(BroadcastMessages).Start();
		}

		public IListener Subscribe(IProcess subscriber)
		{
			var listener = new Listener(subscriber);
			subscriptions.Add(listener);

			return listener;
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
				foreach (var subscription in subscriptions.AsParallel())
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
				foreach (var subscription in subscriptions.AsParallel().Where(l => l.Subscriber.Id == forwardRequest.Recipient.Id))
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