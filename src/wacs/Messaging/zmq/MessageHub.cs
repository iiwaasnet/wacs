using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using wacs.Configuration;
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
        private readonly ConcurrentDictionary<Listener, object> subscriptions;
        private readonly ConcurrentDictionary<PollItem, object> pollItems;
        private readonly BlockingCollection<MultipartMessage> messageQueue;
        private readonly CancellationTokenSource cancellationSource;
        private readonly ConcurrentDictionary<string, NodeConnection> listeningConnections;
        private readonly INode localNode;

        public MessageHub(ISynodConfigurationProvider configProvider, ILogger logger)
        {
            this.configProvider = configProvider;
            this.logger = logger;
            localNode = configProvider.LocalNode;
            messageQueue = new BlockingCollection<MultipartMessage>(new ConcurrentQueue<MultipartMessage>());
            listeningConnections = new ConcurrentDictionary<string, NodeConnection>();
            cancellationSource = new CancellationTokenSource();

            context = new Context();
            subscriptions = new ConcurrentDictionary<Listener, object>();
            pollItems = new ConcurrentDictionary<PollItem, object>();

            multicastListener = CreateMulticastListener();
            unicastListener = CreateUnicastListener(configProvider);
            ConnectListeningSockets(configProvider.World.Select(n => n.Address));

            sender = CreateSender();

            this.configProvider.WorldChanged += OnWorldChanged;
            new Thread(() => PollReceivers(cancellationSource.Token)).Start();
            new Thread(() => ForwardMessagesToListeners(cancellationSource.Token)).Start();
        }

        private void OnWorldChanged()
        {
            var world = configProvider.World.Select(w => w.Address);
            var dead = listeningConnections.Where(c => !world.Contains(c.Key));
            var newNodes = world.Where(w => !listeningConnections.ContainsKey(w));

            ConnectListeningSockets(newNodes);
            CloseDeadSockets(dead);
        }

        private void CloseDeadSockets(IEnumerable<KeyValuePair<string, NodeConnection>> deadSockets)
        {
            object obj;
            foreach (var deadSocket in deadSockets)
            {
                foreach (var pollItem in deadSocket.Value.PollItems)
                {
                    pollItem.PollInHandler -= PollInMessageHandler;
                    pollItems.TryRemove(pollItem, out obj);
                }
                foreach (var socket in deadSocket.Value.Sockets)
                {
                    socket.Dispose();
                }

                NodeConnection con;
                listeningConnections.TryRemove(deadSocket.Key, out con);
            }
        }

        private void ConnectListeningSockets(IEnumerable<string> newNodes)
        {
            foreach (var newNode in newNodes)
            {
                var nodeConnection = new NodeConnection();
                if (listeningConnections.TryAdd(newNode, nodeConnection))
                {
                    nodeConnection.Sockets = new[]
                                             {
                                                 CreateMulticastListener(),
                                                 CreateUnicastListener(configProvider)
                                             };
                    nodeConnection.PollItems = SubscribeListeningSockets(nodeConnection.Sockets).ToArray();

                    foreach (var pollItem in nodeConnection.PollItems)
                    {
                        pollItems.TryAdd(pollItem, null);
                    }

                    foreach (var socket in nodeConnection.Sockets)
                    {
                        socket.Connect(newNode);
                    }
                }
            }
        }

        private Socket CreateUnicastListener(ISynodConfigurationProvider configProvider)
        {
            var socket = context.Socket(SocketType.SUB);
            socket.Subscribe(configProvider.LocalProcess.Id.GetBytes());

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
            socket.Bind(localNode.Address);

            return socket;
        }

        private void ForwardMessagesToListeners(CancellationToken token)
        {
            foreach (var message in messageQueue.GetConsumingEnumerable(token))
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

            messageQueue.Dispose();
        }

        public void Dispose()
        {
            cancellationSource.Cancel(false);
            multicastListener.Dispose();
            unicastListener.Dispose();
            sender.Dispose();
            context.Dispose();
        }

        public IListener Subscribe()
        {
            var listener = new Listener(Unsubscribe);
            subscriptions.TryAdd(listener, null);

            return listener;
        }

        private void Unsubscribe(Listener listener)
        {
            object obj;
            subscriptions.TryRemove(listener, out obj);
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

        private void PollReceivers(CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    context.Poll(pollItems.Keys.ToArray());
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
            logger.InfoFormat("MSG SENT: {0} {1}", multipartMessage.GetMessageType(), multipartMessage.GetSenderId());
            sender.Send(multipartMessage.GetFilterBytes(), SendRecvOpt.NOBLOCK, SendRecvOpt.SNDMORE);
            sender.Send(multipartMessage.GetSenderIdBytes(), SendRecvOpt.NOBLOCK, SendRecvOpt.SNDMORE);
            sender.Send(multipartMessage.GetMessageTypeBytes(), SendRecvOpt.NOBLOCK, SendRecvOpt.SNDMORE);
            sender.Send(multipartMessage.GetMessage(), SendRecvOpt.NOBLOCK);
        }

        private class NodeConnection
        {
            internal IEnumerable<Socket> Sockets { get; set; }
            internal IEnumerable<PollItem> PollItems { get; set; }
        }
    }
}