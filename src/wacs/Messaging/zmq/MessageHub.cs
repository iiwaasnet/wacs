using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using wacs.core;
using wacs.Diagnostics;
using wacs.Paxos.Interface;
using ZMQ;
using Exception = System.Exception;

namespace wacs.Messaging.zmq
{
    public class MessageHub : IMessageHub
    {
        private readonly Context context;
        private readonly Socket multicastListener;
        private readonly Socket unicastListener;
        private readonly Socket sender;
        private readonly ISynodConfigurationProvider configProvider;
        private readonly ILogger logger;
        private readonly ConcurrentBag<Listener> subscriptions;
        private readonly BlockingCollection<MultipartMessage> messageQueue;
        private readonly CancellationTokenSource cancellationSource;
        private readonly string localEndpoint;

        public MessageHub(ISynodConfigurationProvider configProvider, ILogger logger)
        {
            this.configProvider = configProvider;
            this.logger = logger;
            localEndpoint = configProvider.World.GetLocalEndpoint();
            messageQueue = new BlockingCollection<MultipartMessage>(new ConcurrentQueue<MultipartMessage>());
            cancellationSource = new CancellationTokenSource();

            new Thread(() => ForwardMessagesToListeners(cancellationSource.Token)).Start();
            context = new Context();
            subscriptions = new ConcurrentBag<Listener>();

            multicastListener = CreateMulticastListener();
            unicastListener = CreateUnicastListener(configProvider);
            StartListening(new[] {multicastListener, unicastListener}, cancellationSource.Token);
            sender = CreateSender();
        }

        private void StartListening(IEnumerable<Socket> sockets, CancellationToken cancellationToken)
        {
            new Thread(() => PollReceivers(SubscribeListeningSockets(sockets).ToArray(), cancellationToken)).Start();

            ConnectToListeners(sockets, configProvider.World);
        }

        private Socket CreateUnicastListener(ISynodConfigurationProvider configProvider)
        {
            var socket = context.Socket(SocketType.SUB);
            socket.Subscribe(configProvider.LocalNode.Id.GetBytes());

            return socket;
        }

        private Socket CreateMulticastListener()
        {
            var socket = context.Socket(SocketType.SUB);
            socket.Subscribe(MultipartMessage.MulticastId);

            return socket;
        }

        private Socket CreateSender()
        {
            var socket = context.Socket(SocketType.PUB);
            socket.Bind(localEndpoint);

            return socket;
        }

        private void ForwardMessagesToListeners(CancellationToken token)
        {
            foreach (var message in messageQueue.GetConsumingEnumerable(token))
            {
                var msg = new Message(new Envelope {Sender = new Node(message.GetSenderId())},
                                      new Body
                                      {
                                          MessageType = message.GetMessageType(),
                                          Content = message.GetMessage()
                                      });
                foreach (var subscription in subscriptions)
                {
                    subscription.Notify(msg);
                }
            }

            messageQueue.Dispose();
        }

        public void Dispose()
        {
            cancellationSource.Cancel(false);
            multicastListener.Unsubscribe(MultipartMessage.MulticastId);
            multicastListener.Dispose();
            unicastListener.Dispose();
            sender.Dispose();
            context.Dispose();
        }

        public IListener Subscribe()
        {
            var listener = new Listener();
            subscriptions.Add(listener);

            return listener;
        }

        private IEnumerable<PollItem> SubscribeListeningSockets(IEnumerable<Socket> listeningSockets)
        {
            foreach (var listeningSocket in listeningSockets)
            {
                var poller = listeningSocket.CreatePollItem(IOMultiPlex.POLLIN);
                poller.PollInHandler += PollInMessageHandler;

                yield return poller;
            }
        }

        private void PollReceivers(PollItem[] pollItems, CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    context.Poll(pollItems);
                }
            }
            catch (Exception err)
            {
                logger.Error(err);
            }
        }

        private void PollInMessageHandler(Socket socket, IOMultiPlex revents)
        {
            var queue = socket.RecvAll();

            if (queue.Any())
            {
                var multipartMessage = new MultipartMessage(queue);
                logger.InfoFormat("MSG RECEIVED: {0} {1}", multipartMessage.GetMessageType(), multipartMessage.GetSenderId());
                messageQueue.Add(multipartMessage);
            }
        }

        private void ConnectToListeners(IEnumerable<Socket> sockets, IEnumerable<Configuration.INode> listeners)
        {
            foreach (var socket in sockets)
            {
                foreach (var ipEndPoint in listeners)
                {
                    socket.Connect(ipEndPoint.Address);
                }
            }
        }

        public void Broadcast(IMessage message)
        {
            var multipartMessage = new MultipartMessage(null, message);

            SendMessage(multipartMessage);
        }

        public void Send(INode recipient, IMessage message)
        {
            var multipartMessage = new MultipartMessage(recipient, message);

            SendMessage(multipartMessage);
        }

        private void SendMessage(MultipartMessage multipartMessage)
        {
            logger.InfoFormat("MSG SENT: {0} {1}", multipartMessage.GetMessageType(), multipartMessage.GetSenderId());
            sender.Send(multipartMessage.GetFilterBytes(), SendRecvOpt.NOBLOCK, SendRecvOpt.SNDMORE);
            sender.Send(multipartMessage.GetSenderIdBytes(), SendRecvOpt.NOBLOCK, SendRecvOpt.SNDMORE);
            sender.Send(multipartMessage.GetMessageTypeBytes(), SendRecvOpt.NOBLOCK, SendRecvOpt.SNDMORE);
            sender.Send(multipartMessage.GetMessage(), SendRecvOpt.NOBLOCK);
        }
    }
}