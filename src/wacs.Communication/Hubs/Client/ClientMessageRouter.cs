using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Castle.Core.Internal;
using wacs.Configuration;
using wacs.Diagnostics;
using wacs.Framework.State;
using wacs.Messaging.Messages;
using ZeroMQ;
using Process = wacs.Messaging.Messages.Process;

namespace wacs.Communication.Hubs.Client
{
    public class ClientMessageRouter : IClientMessageRouter
    {
        private readonly IClientMessageHubConfiguration config;
        private readonly ILogger logger;
        private readonly BlockingCollection<IAwaitableResponse<IMessage>> forwardQueue;
        private readonly IEnumerable<Thread> forwardingThreads;
        private readonly CancellationTokenSource cancellationSource;
        private readonly IRsmConfiguration rsmConfiguration;
        private readonly ZmqContext context;

        public ClientMessageRouter(IClientMessageHubConfiguration config,
                                   IRsmConfiguration rsmConfiguration,
                                   ILogger logger)
        {
            this.logger = logger;
            this.config = config;
            this.rsmConfiguration = rsmConfiguration;
            cancellationSource = new CancellationTokenSource();
            forwardQueue = new BlockingCollection<IAwaitableResponse<IMessage>>(new ConcurrentQueue<IAwaitableResponse<IMessage>>());
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
            while (!token.IsCancellationRequested)
            {
                try
                {
                    using (var socket = context.CreateSocket(SocketType.REQ))
                    {
                        socket.Linger = TimeSpan.Zero;
                        socket.SendHighWatermark = 100;
                        socket.ReceiveHighWatermark = 200;

                        foreach (var queuedRequest in forwardQueue.GetConsumingEnumerable())
                        {
                            ForwardMessagesToLeader(socket, queuedRequest);
                        }
                    }
                }
                catch (Exception err)
                {
                    logger.Error(err.ToString());
                }
            }

            logger.WarnFormat("Forwarding thread {0} terminated", Thread.CurrentThread.ManagedThreadId);
        }

        private void ForwardMessagesToLeader(ZmqSocket socket, IAwaitableResponse<IMessage> queuedRequest)
        {
            var forwardRequest = (AwaitableResponse) queuedRequest;

            ConnectToLeaderIfChanged(socket, forwardRequest);

            var requestMessage = new ClientMultipartMessage(forwardRequest.Request);
            socket.SendMessage(new ZmqMessage(requestMessage.Frames));

            //TODO: receive timeout
            var response = socket.ReceiveMessage(config.ReceiveWaitTimeout);
            //var response = socket.ReceiveMessage();
            if (!response.IsEmpty && response.IsComplete)
            {
                var responseMessage = new ClientMultipartMessage(response);

                forwardRequest.SetResponse(new Message(new Envelope {Sender = new Process {Id = responseMessage.GetSenderId()}},
                                                       new Body
                                                       {
                                                           MessageType = responseMessage.GetMessageType(),
                                                           Content = responseMessage.GetMessage()
                                                       }));
            }
        }

        private static void ConnectToLeaderIfChanged(ZmqSocket socket, AwaitableResponse forwardRequest)
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
            logger.InfoFormat("Request forwarded to leader {0}", leader.GetServiceAddress());

            IAwaitableResponse<IMessage> response = new AwaitableResponse(leader, message);
            forwardQueue.Add(response);

            return response.GetResponse(rsmConfiguration.CommandExecutionTimeout);
        }

        public void Dispose()
        {
            try
            {
                cancellationSource.Cancel(false);
                forwardQueue.CompleteAdding();
                forwardingThreads.ForEach(t => t.Join());
                context.Dispose();
                cancellationSource.Dispose();
                forwardQueue.Dispose();
            }
            catch (Exception err)
            {
                logger.Error(err);
            }
        }
    }
}