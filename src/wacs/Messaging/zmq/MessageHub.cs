using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
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
        private readonly IMessageHubConfiguration config;
        private readonly ILogger logger;
        private IProcess subscriber;
        private readonly ConcurrentBag<Listener> subscriptions;
        private readonly BlockingCollection<MultipartMessage> messageQueue;

        public MessageHub(IMessageHubConfiguration config, ILogger logger)
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
            ConnectToListeners(new[] {multicastListener, unicastListener}, config.Listeners);

            var listener = new Listener(subscriber);
            subscriptions.Add(listener);

            return listener;
        }

        private void BindSenderToSocket(IProcess subscriber)
        {
            sender.Identity = subscriber.Id.GetBytes();
            sender.Bind(config.Sender.ToString());
        }

        private void SubscribeListeningSockets(IProcess subscriber)
        {
            unicastListener.Subscribe(subscriber.Id.GetBytes());
            var unicastPoller = unicastListener.CreatePollItem(IOMultiPlex.POLLIN);
            unicastPoller.PollInHandler += PollInMessageHandler;

            multicastListener.Subscribe(MultipartMessage.MulticastId);
            var multicastPoller = multicastListener.CreatePollItem(IOMultiPlex.POLLIN);
            multicastPoller.PollInHandler += PollInMessageHandler;

            new Thread(() => PollReceivers(new []{unicastPoller, multicastPoller})).Start();
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
                messageQueue.Add(new MultipartMessage(queue));
            }
        }

        private void ConnectToListeners(IEnumerable<Socket> sockets, IEnumerable<IPEndPoint> listeners)
        {
            foreach (var ipEndPoint in listeners)
            {
                foreach (var socket in sockets)
                {
                    socket.Identity = subscriber.Id.GetBytes();
                    socket.Connect(ipEndPoint.ToString());
                }
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
            sender.Send(multipartMessage.GetFilterBytes(), SendRecvOpt.NOBLOCK, SendRecvOpt.SNDMORE);
            sender.Send(multipartMessage.GetSenderIdBytes(), SendRecvOpt.NOBLOCK, SendRecvOpt.SNDMORE);
            sender.Send(multipartMessage.GetMessageTypeBytes(), SendRecvOpt.NOBLOCK, SendRecvOpt.SNDMORE);
            sender.Send(multipartMessage.GetMessage(), SendRecvOpt.NOBLOCK);
        }
    }
}