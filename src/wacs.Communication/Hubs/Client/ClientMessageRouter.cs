using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using NetMQ;
using NetMQ.zmq;
using wacs.Configuration;
using wacs.Diagnostics;
using wacs.Framework;
using wacs.Framework.State;
using wacs.Messaging.Messages;
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
        private readonly NetMQContext context;

        public ClientMessageRouter(IClientMessageHubConfiguration config,
            IRsmConfiguration rsmConfiguration,
            ILogger logger)
        {
            this.logger = logger;
            this.config = config;
            this.rsmConfiguration = rsmConfiguration;
            cancellationSource = new CancellationTokenSource();
            forwardQueue =
                new BlockingCollection<IAwaitableResponse<IMessage>>(new ConcurrentQueue<IAwaitableResponse<IMessage>>());
            context = NetMQContext.Create();
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
                    using (var socket = context.CreateSocket(ZmqSocketType.Req))
                    {
                        socket.Options.Linger = TimeSpan.Zero;
                        socket.Options.SendHighWatermark = 100;
                        socket.Options.ReceiveHighWatermark = 200;

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

        private void ForwardMessagesToLeader(NetMQSocket socket, IAwaitableResponse<IMessage> queuedRequest)
        {
            var forwardRequest = (AwaitableResponse) queuedRequest;

            ConnectToLeaderIfChanged(socket, forwardRequest);

            var requestMessage = new ClientMultipartMessage(forwardRequest.Request);
            socket.SendMessage(new NetMQMessage(requestMessage.Frames));

            //TODO: receive timeout
            var response = socket.ReceiveMessage(config.ReceiveWaitTimeout);
            //var response = socket.ReceiveMessage();
            if (response != null && !response.IsEmpty /*&& response.IsComplete*/)
            {
                var responseMessage = new ClientMultipartMessage(response);

                forwardRequest.SetResponse(
                    new Message(new Envelope {Sender = new Process {Id = responseMessage.GetSenderId()}},
                        new Body
                        {
                            MessageType = responseMessage.GetMessageType(),
                            Content = responseMessage.GetMessage()
                        }));
            }
        }

        private static void ConnectToLeaderIfChanged(NetMQSocket socket, AwaitableResponse forwardRequest)
        {
            if (socket.Options.GetLastEndpoint != forwardRequest.Leader.GetServiceAddress())
            {
                if (!string.IsNullOrWhiteSpace(socket.Options.GetLastEndpoint))
                {
                    socket.Disconnect(socket.Options.GetLastEndpoint);
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