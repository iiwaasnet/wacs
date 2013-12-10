using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using wacs.Configuration;
using wacs.core;
using wacs.Diagnostics;
using ZMQ;
using Exception = System.Exception;
using Socket = ZMQ.Socket;
using SocketType = ZMQ.SocketType;

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
        private readonly ConcurrentBag<Listener> subscriptions;
        private readonly BlockingCollection<MultipartMessage> messageQueue;
        private readonly CancellationTokenSource cancellationSource;
        private readonly string localEndpoint;

        public MessageHub(ISynodConfiguration config, ILogger logger)
        {
            this.config = config;
            this.logger = logger;
            localEndpoint = config.Nodes.GetLocalEndpoint();
            messageQueue = new BlockingCollection<MultipartMessage>(new ConcurrentQueue<MultipartMessage>());
            cancellationSource = new CancellationTokenSource();

            new Thread(() => ForwardMessagesToListeners(cancellationSource.Token)).Start();
            context = new Context();
            subscriptions = new ConcurrentBag<Listener>();

            multicastListener = context.Socket(SocketType.SUB);
            unicastListener = context.Socket(SocketType.SUB);
            sender = context.Socket(SocketType.PUB);
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

        public IListener Subscribe(IProcess subscriber)
        {
            BindSenderToSocket();

            SubscribeListeningSockets(subscriber);
            ConnectToListeners(new[] {unicastListener, multicastListener}, config.Nodes);

            var listener = new Listener(subscriber);
            subscriptions.Add(listener);

            return listener;
        }

        private void BindSenderToSocket()
        {
            sender.Bind(localEndpoint);
        }

        private void SubscribeListeningSockets(IProcess subscriber)
        {
            unicastListener.Subscribe(subscriber.Id.GetBytes());
            var unicastPoller = unicastListener.CreatePollItem(IOMultiPlex.POLLIN);
            unicastPoller.PollInHandler += PollInMessageHandler;

            multicastListener.Subscribe(MultipartMessage.MulticastId);
            var multicastPoller = multicastListener.CreatePollItem(IOMultiPlex.POLLIN);
            multicastPoller.PollInHandler += PollInMessageHandler;

            new Thread(() => PollReceivers(new[] {unicastPoller, multicastPoller}, cancellationSource.Token)).Start();
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

        private void ConnectToListeners(IEnumerable<Socket> sockets, IEnumerable<INode> listeners)
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
    }
}