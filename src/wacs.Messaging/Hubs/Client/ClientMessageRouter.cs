using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Castle.Core.Internal;
using Castle.Core.Logging;
using wacs.Configuration;
using wacs.Framework.State;
using wacs.Messaging.Hubs.Intercom;
using wacs.Messaging.Messages;
using ZeroMQ;

namespace wacs.Messaging.Hubs.Client
{
    public class ClientMessageRouter : IClientMessageRouter
    {
        private readonly IClientMessageHubConfiguration config;
        private readonly ILogger logger;
        private readonly IClientMessagesRepository messagesRepository;
        private readonly BlockingCollection<IAwaitableResult<IMessage>> forwardQueue;
        private readonly IEnumerable<Thread> forwardingThreads;
        private readonly CancellationTokenSource cancellationSource;
        private readonly ZmqContext context;

        public ClientMessageRouter(IClientMessagesRepository messagesRepository,
                                   ISynodConfigurationProvider configurationProvider,
                                   IClientMessageHubConfiguration config,
                                   ILogger logger)
        {
            this.logger = logger;
            this.config = config;
            this.messagesRepository = messagesRepository;
            cancellationSource = new CancellationTokenSource();
            forwardQueue = new BlockingCollection<IAwaitableResult<IMessage>>(new ConcurrentQueue<IAwaitableResult<IMessage>>());
            context = ZmqContext.Create();
            forwardingThreads = StartForwardingThreads().ToArray();
        }

        private IEnumerable<Thread> StartForwardingThreads()
        {
            for (var i = 0; i < config.ParallelMessageProcessors; i++)
            {
                var thread = new Thread(() => ForwardMessages(cancellationSource.Token));
                thread.Start();

                yield return thread;
            }
        }

        private void ForwardMessages(CancellationToken token)
        {
            using (var socket = context.CreateSocket(SocketType.REQ))
            {
                while (!token.IsCancellationRequested)
                {
                    try
                    {
                        var queuedRequest = forwardQueue.Take();

                        ForwardMessagesToLeader(socket, queuedRequest);
                    }
                    catch (Exception err)
                    {
                        logger.Error(err.ToString);
                    }
                }
            }
            logger.WarnFormat("Forwarding thread {0} terminated", Thread.CurrentThread.ManagedThreadId);
        }

        private void ForwardMessagesToLeader(ZmqSocket socket, IAwaitableResult<IMessage> queuedRequest)
        {
            var forwardRequest = (ClientRequestAwaitable) queuedRequest;

            ConnectToLeaderIfChanged(socket, forwardRequest);

            socket.Send(new MultipartMessage(??? have Intercom and client messages))
        }

        private static void ConnectToLeaderIfChanged(ZmqSocket socket, ClientRequestAwaitable forwardRequest)
        {
            if (socket.LastEndpoint != forwardRequest.Leader.GetServiceAddress())
            {
                if (!string.IsNullOrWhiteSpace(socket.LastEndpoint))
                {
                    socket.Disconnect(socket.LastEndpoint);
                }
                socket.Connect(forwardRequest.Leader.GetServiceAddress());
            }
        }

        public IMessage ForwardClientRequestToLeader(INode leader, IMessage message)
        {
            IAwaitableResult<IMessage> response = new ClientRequestAwaitable(leader, message);
            forwardQueue.Add(response);

            return response.GetResult();
        }

        public bool MessageRequiresLidership(IMessage message)
        {
            return messagesRepository.RequiresQuorum(message);
        }

        public void Dispose()
        {
            cancellationSource.Cancel(false);
            forwardingThreads.ForEach(t => t.Join());
            context.Dispose();
        }
    }
}