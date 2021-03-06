﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using NetMQ;
using NetMQ.zmq;
using wacs.Configuration;
using wacs.Diagnostics;
using wacs.Messaging.Messages;
using Poller = NetMQ.Poller;

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
        private readonly NetMQContext multicastContext;
        private readonly NetMQSocket multicastListener;
        private readonly Poller multicastPoller;
        private readonly BlockingCollection<IntercomMultipartMessage> outMessageQueue;
        private readonly NetMQSocket sender;
        private readonly NetMQContext senderContext;
        private readonly TimeSpan socketsPollTimeout;
        private readonly ConcurrentDictionary<Listener, object> subscriptions;
        private readonly NetMQContext unicastContext;
        private readonly NetMQSocket unicastListener;
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

            senderContext = NetMQContext.Create();
            unicastContext = NetMQContext.Create();
            multicastContext = NetMQContext.Create();

            subscriptions = new ConcurrentDictionary<Listener, object>();

            multicastListener = CreateMulticastListener(multicastContext);
            unicastListener = CreateUnicastListener(unicastContext, configProvider.LocalProcess);
            currentWorld = new HashSet<INode>(configProvider.World);
            ConnectListeningSockets(currentWorld);

            unicastPoller = new Poller(unicastListener);
            multicastPoller = new Poller(multicastListener);

            sender = CreateSender(senderContext);

            this.configProvider.WorldChanged += OnWorldChanged;
            new Thread(() => PollReceivers(multicastPoller, cancellationSource.Token)).Start();
            new Thread(() => PollReceivers(unicastPoller, cancellationSource.Token)).Start();
            new Thread(ForwardIncomingMessages).Start();
            new Thread(ForwardOutgoingSenders).Start();
        }

        public void Dispose()
        {
            cancellationSource.Cancel(false);
            multicastPoller.Stop(true);
            unicastPoller.Stop(true);
            
            outMessageQueue.CompleteAdding();
            inMessageQueue.CompleteAdding();

            multicastListener.Dispose();
            unicastListener.Dispose();
            sender.Dispose();

            unicastPoller.Dispose();
            multicastPoller.Dispose();

            senderContext.Dispose();
            unicastContext.Dispose();
            multicastContext.Dispose();
            cancellationSource.Dispose();
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

        private void ForwardOutgoingSenders()
        {
            foreach (var message in outMessageQueue.GetConsumingEnumerable())
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

        private NetMQSocket CreateUnicastListener(NetMQContext context, IProcess localProcess)
        {
            return CreateListeningSocket(context, localProcess.Id.GetBytes());
        }

        private NetMQSocket CreateMulticastListener(NetMQContext context)
        {
            return CreateListeningSocket(context, IntercomMultipartMessage.MulticastId);
        }

        private NetMQSocket CreateListeningSocket(NetMQContext context, byte[] prefix)
        {
            var socket = context.CreateSocket(ZmqSocketType.Sub);
            socket.Options.ReceiveHighWatermark = 200;
            socket.Options.Linger = TimeSpan.Zero;
            socket.Subscribe(prefix);
            socket.ReceiveReady += SocketOnReceiveReady;

            return socket;
        }

        private void SocketOnReceiveReady(object sender, NetMQSocketEventArgs socketEventArgs)
        {
            try
            {
                var timer = new Stopwatch();
                timer.Start();

                var message = socketEventArgs.Socket.ReceiveMessage(TimeSpan.FromMilliseconds(50));

                //if (message.IsComplete)
                //{
                    var multipartMessage = new IntercomMultipartMessage(message);
                    //logger.InfoFormat("Msg received: {0} sender: {1}", multipartMessage.GetMessageType(), multipartMessage.GetSenderId());
                    inMessageQueue.Add(multipartMessage);
                //}

                timer.Stop();
                logger.InfoFormat("Msg received in {0} msec", timer.ElapsedMilliseconds);
                //logger.InfoFormat("Backlog:{0} Receive bfr:{1}",
                //                  socketEventArgs.Socket.Backlog,
                //                  socketEventArgs.Socket.ReceiveBufferSize);
            }
            catch (Exception err)
            {
                logger.Error(err);
            }
        }

        private NetMQSocket CreateSender(NetMQContext context)
        {
            var socket = context.CreateSocket(ZmqSocketType.Pub);
            socket.Options.SendHighWatermark = 100;
            socket.Options.Linger = TimeSpan.Zero;
            socket.Bind(localNode.GetIntercomAddress());

            return socket;
        }

        //private void SocketOnReceiveReady(object sender, SocketEventArgs socketEventArgs)
        //{
        //    try
        //    {
        //        var timer = new Stopwatch();
        //        timer.Start();

        //        var message = socketEventArgs.Socket.ReceiveMessage(TimeSpan.FromMilliseconds(50));

        //        if (message.IsComplete)
        //        {
        //            var multipartMessage = new IntercomMultipartMessage(message);
        //            //logger.InfoFormat("Msg received: {0} sender: {1}", multipartMessage.GetMessageType(), multipartMessage.GetSenderId());
        //            inMessageQueue.Add(multipartMessage);
        //        }

        //        timer.Stop();
        //        logger.InfoFormat("Msg received in {0} msec", timer.ElapsedMilliseconds);
        //        //logger.InfoFormat("Backlog:{0} Receive bfr:{1}",
        //        //                  socketEventArgs.Socket.Backlog,
        //        //                  socketEventArgs.Socket.ReceiveBufferSize);
        //    }
        //    catch (Exception err)
        //    {
        //        logger.Error(err);
        //    }
        //}

        private void ForwardIncomingMessages()
        {
            foreach (var message in inMessageQueue.GetConsumingEnumerable())
            {
                try
                {
                    var msg = new Message(new Envelope {Sender = new Messaging.Messages.Process{Id = message.GetSenderId()}},
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
                    poller.Start();
                    //poller.Poll(socketsPollTimeout);
                }
            }
            catch (Exception err)
            {
                logger.Error(err);
            }
        }

        private void SendMessage(IntercomMultipartMessage multipartMessage)
        {
            var timer = new Stopwatch();
            timer.Start();
            //logger.InfoFormat("Msg sent: {0} sender: {1}", multipartMessage.GetMessageType(), multipartMessage.GetSenderId());

            var message = new NetMQMessage(multipartMessage.Frames);
            sender.SendMessage(message);

            timer.Stop();
            logger.InfoFormat("Msg sent in {0}", timer.ElapsedMilliseconds);
        }
    }
}