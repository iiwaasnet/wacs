using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using wacs.Configuration;
using wacs.Diagnostics;
using wacs.FLease.Messages;
using ZMQ;

namespace wacs.Messaging.zmq
{
    public class MessageHub : IMessageHub
    {
        private readonly Context context;
        private readonly Socket multicastListener;
        private readonly Socket unicastListener;
        private readonly Socket sender;
        private readonly ISynodConfiguration config;
        private readonly ILogger logger;
        private IProcess subscriber;
        private readonly ConcurrentBag<Listener> subscriptions;
        private readonly BlockingCollection<MultipartMessage> messageQueue;

        public MessageHub(ISynodConfiguration config, ILogger logger)
        {
            this.config = config;
            this.logger = logger;
            messageQueue = new BlockingCollection<MultipartMessage>(new ConcurrentQueue<MultipartMessage>());
            new Thread(ForwardMessagesToListeners).Start();
            context = new Context();
            subscriptions = new ConcurrentBag<Listener>();

            multicastListener = context.Socket(SocketType.SUB);
            unicastListener = context.Socket(SocketType.SUB);
            sender = context.Socket(SocketType.PUB);
        }

        private void ForwardMessagesToListeners()
        {
            foreach (var message in messageQueue.GetConsumingEnumerable())
            {
                var msg = new Message
                          {
                              Envelope = new Envelope {Sender = new Sender {Process = new Process(message.GetSenderId())}},
                              Body = new Body
                                     {
                                         MessageType = message.GetMessageType(),
                                         Content = message.GetMessage()
                                     }
                          };
                foreach (var subscription in subscriptions)
                {
                    subscription.Notify(msg);
                }
            }
        }

        public void Dispose()
        {
            multicastListener.Unsubscribe(MultipartMessage.MulticastId);
            multicastListener.Dispose();
            unicastListener.Dispose();
            sender.Dispose();
            context.Dispose();
        }

        public IListener Subscribe(IProcess subscriber)
        {
            this.subscriber = subscriber;

            BindSenderToSocket(subscriber);

            SubscribeListeningSockets(subscriber);
            ConnectToListeners(unicastListener , config.Nodes, subscriber.Id.GetBytes());
            ConnectToListeners(multicastListener, config.Nodes, MultipartMessage.MulticastId);

            var listener = new Listener(subscriber);
            subscriptions.Add(listener);

            return listener;
        }

        private void BindSenderToSocket(IProcess subscriber)
        {
            sender.Identity = subscriber.Id.GetBytes();
            sender.Bind(config.This.Address);
        }

        private void SubscribeListeningSockets(IProcess subscriber)
        {
            unicastListener.Subscribe(subscriber.Id.GetBytes());
            var unicastPoller = unicastListener.CreatePollItem(IOMultiPlex.POLLIN);
            unicastPoller.PollInHandler += PollInMessageHandler;

            multicastListener.Subscribe(MultipartMessage.MulticastId);
            var multicastPoller = multicastListener.CreatePollItem(IOMultiPlex.POLLIN);
            multicastPoller.PollInHandler += PollInMessageHandler;

            new Thread(() => PollReceivers(new[] {unicastPoller, multicastPoller})).Start();
        }

        private void PollMultiInMessageHandler(Socket socket, IOMultiPlex revents)
        {
            PollInMessageHandler(socket, revents);
        }

        private void PollReceivers(PollItem[] pollItems)
        {
            while (true)
            {
                context.Poll(pollItems);
            }
        }

        private void PollInMessageHandler(Socket socket, IOMultiPlex revents)
        {
            var queue = socket.RecvAll();

            if (queue.Any())
            {
                var multipartMessage = new MultipartMessage(queue);
                logger.InfoFormat("MSG RECEIVED: {0} {1}", multipartMessage.GetMessageType(), multipartMessage.GetMessage().GetString());
                messageQueue.Add(multipartMessage);
            }
        }

        private void ConnectToListeners(Socket socket, IEnumerable<INode> listeners, byte[] identity)
        {
            foreach (var ipEndPoint in listeners)
            {
                socket.Identity = identity;
                socket.Connect(ipEndPoint.Address);
            }
        }

        public void Broadcast(IMessage message)
        {
            var multipartMessage = new MultipartMessage(null, message);

            SendMessage(multipartMessage);
        }

        public void Send(IProcess recipient, IMessage message)
        {
            var multipartMessage = new MultipartMessage(recipient, message);

            SendMessage(multipartMessage);
        }

        private void SendMessage(MultipartMessage multipartMessage)
        {
            logger.InfoFormat("MSG SENT: {0} {1}", multipartMessage.GetMessageType(), multipartMessage.GetMessage().GetString());
            sender.Send(multipartMessage.GetFilterBytes(), SendRecvOpt.NOBLOCK, SendRecvOpt.SNDMORE);
            sender.Send(multipartMessage.GetSenderIdBytes(), SendRecvOpt.NOBLOCK, SendRecvOpt.SNDMORE);
            sender.Send(multipartMessage.GetMessageTypeBytes(), SendRecvOpt.NOBLOCK, SendRecvOpt.SNDMORE);
            sender.Send(multipartMessage.GetMessage(), SendRecvOpt.NOBLOCK);
        }
    }
}