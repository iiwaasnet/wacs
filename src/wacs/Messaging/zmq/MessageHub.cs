using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using wacs.Configuration;
using wacs.Diagnostics;
using wacs.Paxos.Interface;
using ZeroMQ;

namespace wacs.Messaging.zmq
{
    public class MessageHub : IMessageHub
    {
        private readonly ZmqContext context;
        private readonly ZmqSocket multicastListener;
        private readonly ZmqSocket unicastListener;
        private readonly ZmqSocket sender;
        private readonly ISynodConfigurationProvider configProvider;
        private readonly ILogger logger;
        private readonly ConcurrentDictionary<Listener, object> subscriptions;
        private HashSet<INode> currentWorld;
        private readonly Poller poller;
        private readonly BlockingCollection<MultipartMessage> messageQueue;
        private readonly CancellationTokenSource cancellationSource;
        private readonly INode localNode;
        private readonly object locker = new object();
        // TODO: create a setting for timeout
        private readonly TimeSpan socketsPollTimeout;

        public MessageHub(ISynodConfigurationProvider configProvider, ILogger logger)
        {
            socketsPollTimeout = TimeSpan.FromSeconds(3);
            this.configProvider = configProvider;
            this.logger = logger;
            localNode = configProvider.LocalNode;
            messageQueue = new BlockingCollection<MultipartMessage>(new ConcurrentQueue<MultipartMessage>());
            cancellationSource = new CancellationTokenSource();

            context = ZmqContext.Create();
            subscriptions = new ConcurrentDictionary<Listener, object>();

            multicastListener = CreateMulticastListener();
            unicastListener = CreateUnicastListener(configProvider.LocalProcess);
            currentWorld = new HashSet<INode>(configProvider.World);
            ConnectListeningSockets(currentWorld);

            poller = new Poller(new[] {multicastListener, unicastListener});

            sender = CreateSender();

            this.configProvider.WorldChanged += OnWorldChanged;
            new Thread(() => PollReceivers(cancellationSource.Token)).Start();
            new Thread(() => ForwardMessagesToListeners(cancellationSource.Token)).Start();          
        }

        private void OnWorldChanged()
        {
            lock (locker)
            {
                var previousWorld = currentWorld;
                currentWorld = new HashSet<INode>(configProvider.World);

                var dead = previousWorld.Where(pw => !currentWorld.Contains(pw));
                var newNodes = currentWorld.Where(cw => !previousWorld.Contains(cw));

                ConnectListeningSockets(newNodes);
                CloseDeadSockets(dead);
            }
        }

        private void CloseDeadSockets(IEnumerable<INode> deadNodes)
        {
            foreach (var deadNode in deadNodes)
            {
                multicastListener.Disconnect(deadNode.Address);
                unicastListener.Disconnect(deadNode.Address);
            }
        }

        private void ConnectListeningSockets(IEnumerable<INode> nodes)
        {
            foreach (var node in nodes)
            {
                multicastListener.Connect(node.Address);
                unicastListener.Connect(node.Address);
            }
        }

        private ZmqSocket CreateUnicastListener(IProcess localProcess)
        {
            var socket = context.CreateSocket(SocketType.SUB);
            socket.Subscribe(localProcess.Id.GetBytes());
            socket.ReceiveReady += SocketOnReceiveReady;

            return socket;
        }

        private ZmqSocket CreateMulticastListener()
        {
            var socket = context.CreateSocket(SocketType.SUB);
            socket.Subscribe(MultipartMessage.MulticastId);
            socket.ReceiveReady += SocketOnReceiveReady;

            return socket;
        }

        private ZmqSocket CreateSender()
        {
            var socket = context.CreateSocket(SocketType.PUB);
            socket.Bind(localNode.Address);

            return socket;
        }

        private void SocketOnReceiveReady(object sender, SocketEventArgs socketEventArgs)
        {
            var message = socketEventArgs.Socket.ReceiveMessage();

            if (message.IsComplete)
            {
                var multipartMessage = new MultipartMessage(message);
                logger.InfoFormat("Msg received: {0} sender: {1}", multipartMessage.GetMessageType(), multipartMessage.GetSenderId());
                messageQueue.Add(multipartMessage);
            }
        }

        private void ForwardMessagesToListeners(CancellationToken token)
        {
            foreach (var message in messageQueue.GetConsumingEnumerable(token))
            {
                try
                {
                    var msg = new Message(new Envelope {Sender = new Process(message.GetSenderId())},
                                          new Body
                                          {
                                              MessageType = message.GetMessageType(),
                                              Content = message.GetMessage()
                                          });
                    foreach (var subscription in subscriptions.Keys)
                    {
                        subscription.Notify(msg);
                    }
                }
                catch (Exception err)
                {
                    logger.Error(err);
                }
            }

            messageQueue.Dispose();
        }

        public void Dispose()
        {
            cancellationSource.Cancel(false);
            multicastListener.Dispose();
            unicastListener.Dispose();
            sender.Dispose();
            poller.Dispose();
            context.Dispose();
        }

        public IListener Subscribe()
        {
            var listener = new Listener(Unsubscribe, logger);
            subscriptions.TryAdd(listener, null);

            return listener;
        }

        private void Unsubscribe(Listener listener)
        {
            object obj;
            subscriptions.TryRemove(listener, out obj);
        }

        private void PollReceivers(CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested)
                {             
                    poller.Poll(socketsPollTimeout);
                }
            }
            catch (Exception err)
            {
                logger.Error(err);
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
            logger.InfoFormat("Msg sent: {0} sender: {1}", multipartMessage.GetMessageType(), multipartMessage.GetSenderId());
            var message = new ZmqMessage(multipartMessage.Frames);
            sender.SendMessage(message);
        }
    }
}