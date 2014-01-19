﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using wacs.Configuration;
using wacs.Diagnostics;
using wacs.Messaging.Messages;
using ZeroMQ;

namespace wacs.Communication.Hubs.Intercom
{
    public class IntercomMessageHub : IIntercomMessageHub
    {
        private readonly CancellationTokenSource cancellationSource;
        private readonly ISynodConfigurationProvider configProvider;
        private readonly BlockingCollection<IntercomMultipartMessage> inMessageQueue;
        private readonly INode localNode;
        private readonly object locker = new object();
        private readonly ILogger logger;
        private readonly ZmqContext multicastContext;
        private readonly ZmqSocket multicastListener;
        private readonly Poller multicastPoller;
        private readonly BlockingCollection<IntercomMultipartMessage> outMessageQueue;
        private readonly ZmqSocket sender;
        private readonly ZmqContext senderContext;
        private readonly TimeSpan socketsPollTimeout;
        private readonly ConcurrentDictionary<Listener, object> subscriptions;
        private readonly ZmqContext unicastContext;
        private readonly ZmqSocket unicastListener;
        private readonly Poller unicastPoller;
        private HashSet<INode> currentWorld;

        public IntercomMessageHub(ISynodConfigurationProvider configProvider, ILogger logger)
        {
            socketsPollTimeout = TimeSpan.FromSeconds(3);
            this.configProvider = configProvider;
            this.logger = logger;
            localNode = configProvider.LocalNode;
            inMessageQueue = new BlockingCollection<IntercomMultipartMessage>(new ConcurrentQueue<IntercomMultipartMessage>());
            outMessageQueue = new BlockingCollection<IntercomMultipartMessage>(new ConcurrentQueue<IntercomMultipartMessage>());
            cancellationSource = new CancellationTokenSource();

            senderContext = ZmqContext.Create();
            unicastContext = ZmqContext.Create();
            multicastContext = ZmqContext.Create();

            subscriptions = new ConcurrentDictionary<Listener, object>();

            multicastListener = CreateMulticastListener(multicastContext);
            unicastListener = CreateUnicastListener(unicastContext, configProvider.LocalProcess);
            currentWorld = new HashSet<INode>(configProvider.World);
            ConnectListeningSockets(currentWorld);

            unicastPoller = new Poller(new[] {unicastListener});
            multicastPoller = new Poller(new[] {multicastListener});

            sender = CreateSender(senderContext);

            this.configProvider.WorldChanged += OnWorldChanged;
            new Thread(() => PollReceivers(multicastPoller, cancellationSource.Token)).Start();
            new Thread(() => PollReceivers(unicastPoller, cancellationSource.Token)).Start();
            new Thread(() => ForwardIncomingMessages(cancellationSource.Token)).Start();
            new Thread(() => ForwardOutgoingSenders(cancellationSource.Token)).Start();
        }

        public void Dispose()
        {
            cancellationSource.Cancel(false);

            multicastListener.Dispose();
            unicastListener.Dispose();
            sender.Dispose();

            unicastPoller.Dispose();
            multicastPoller.Dispose();

            senderContext.Dispose();
            unicastContext.Dispose();
            multicastContext.Dispose();
        }

        public IListener Subscribe()
        {
            var listener = new Listener(Unsubscribe, logger);
            subscriptions.TryAdd(listener, null);

            return listener;
        }

        public void Broadcast(IMessage message)
        {
            var multipartMessage = new IntercomMultipartMessage(null, message);

            outMessageQueue.Add(multipartMessage);
        }

        public void Send(IProcess recipient, IMessage message)
        {
            var multipartMessage = new IntercomMultipartMessage(recipient, message);

            outMessageQueue.Add(multipartMessage);
        }

        private void ForwardOutgoingSenders(CancellationToken token)
        {
            foreach (var message in outMessageQueue.GetConsumingEnumerable(token))
            {
                try
                {
                    SendMessage(message);
                }
                catch (Exception err)
                {
                    logger.Error(err);
                }
            }

            outMessageQueue.Dispose();
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
                multicastListener.Disconnect(deadNode.GetIntercomAddress());
                unicastListener.Disconnect(deadNode.GetIntercomAddress());
            }
        }

        private void ConnectListeningSockets(IEnumerable<INode> nodes)
        {
            foreach (var node in nodes)
            {
                multicastListener.Connect(node.GetIntercomAddress());
                unicastListener.Connect(node.GetIntercomAddress());
            }
        }

        private ZmqSocket CreateUnicastListener(ZmqContext context, IProcess localProcess)
        {
            return CreateListeningSocket(context, localProcess.Id.GetBytes());
        }

        private ZmqSocket CreateMulticastListener(ZmqContext context)
        {
            return CreateListeningSocket(context, IntercomMultipartMessage.MulticastId);
        }

        private ZmqSocket CreateListeningSocket(ZmqContext context, byte[] prefix)
        {
            var socket = context.CreateSocket(SocketType.SUB);
            socket.ReceiveHighWatermark = 200;
            socket.Subscribe(prefix);
            socket.ReceiveReady += SocketOnReceiveReady;

            return socket;
        }

        private ZmqSocket CreateSender(ZmqContext context)
        {
            var socket = context.CreateSocket(SocketType.PUB);
            socket.SendHighWatermark = 100;
            socket.Bind(localNode.GetIntercomAddress());

            return socket;
        }

        private void SocketOnReceiveReady(object sender, SocketEventArgs socketEventArgs)
        {
            try
            {
                var timer = new Stopwatch();
                timer.Start();

                var message = socketEventArgs.Socket.ReceiveMessage(TimeSpan.FromMilliseconds(50));

                if (message.IsComplete)
                {
                    var multipartMessage = new IntercomMultipartMessage(message);
                    logger.InfoFormat("Msg received: {0} sender: {1}", multipartMessage.GetMessageType(), multipartMessage.GetSenderId());
                    inMessageQueue.Add(multipartMessage);
                }

                timer.Stop();
                //logger.InfoFormat("Msg queued in {0} msec", timer.ElapsedMilliseconds);
                //logger.InfoFormat("Backlog:{0} Receive bfr:{1}",
                //                  socketEventArgs.Socket.Backlog,
                //                  socketEventArgs.Socket.ReceiveBufferSize);
            }
            catch (Exception err)
            {
                logger.Error(err);
            }
        }

        private void ForwardIncomingMessages(CancellationToken token)
        {
            foreach (var message in inMessageQueue.GetConsumingEnumerable(token))
            {
                try
                {
                    var msg = new Message(new Envelope {Sender = new Configuration.Process(message.GetSenderId())},
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

            inMessageQueue.Dispose();
        }

        private void Unsubscribe(Listener listener)
        {
            object obj;
            subscriptions.TryRemove(listener, out obj);
        }

        private void PollReceivers(Poller poller, CancellationToken token)
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
                logger.ErrorFormat("Sender status:[{0}] Multicast status:[{1}] Unicast status:[{2}] Error:[{3}]",
                                   sender.SendStatus,
                                   multicastListener.ReceiveStatus,
                                   unicastListener.ReceiveStatus,
                                   err);
            }
        }

        private void SendMessage(IntercomMultipartMessage multipartMessage)
        {
            logger.InfoFormat("Msg sent: {0} sender: {1}", multipartMessage.GetMessageType(), multipartMessage.GetSenderId());
            var message = new ZmqMessage(multipartMessage.Frames);
            sender.SendMessage(message);
        }
    }
}