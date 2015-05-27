using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using Castle.Core.Internal;
using NetMQ;
using NetMQ.Devices;
using NetMQ.zmq;
using wacs.Configuration;
using wacs.Diagnostics;
using wacs.Messaging.Messages;
using wacs.Messaging.Messages.Client.Error;
using Process = wacs.Messaging.Messages.Process;

namespace wacs.Communication.Hubs.Client
{
    public class ClientMessageHub : IClientMessageHub
    {
        private const string InprocWorkersAddress = "inproc://processors";
        private Func<IMessage, IMessage> messageHandler;
        private readonly ISynodConfigurationProvider synodConfigProvider;
        private bool disposed;
        private readonly QueueDevice device;
        private readonly NetMQContext context;
        private readonly CancellationTokenSource tokenSource;
        private readonly IClientMessageHubConfiguration config;
        private readonly IEnumerable<Thread> processingThreads;
        private readonly ILogger logger;

        public ClientMessageHub(ISynodConfigurationProvider synodConfigProvider,
                                IClientMessageHubConfiguration config,
                                ILogger logger)
        {
            this.logger = logger;
            tokenSource = new CancellationTokenSource();
            this.synodConfigProvider = synodConfigProvider;
            this.config = config;
            context = NetMQContext.Create();
            //new Thread(AcceptClientRequests).Start();
            device = CreateProcessingDevice();
            processingThreads = CreateRequestProcessingThreads().ToArray();
        }

        private void AcceptClientRequests()
        {
            try
            {
                using (var socket = context.CreateSocket(ZmqSocketType.Rep))
                {
                    socket.Options.SendHighWatermark = 100;
                    socket.Options.ReceiveHighWatermark = 200;
                    socket.Options.Linger = TimeSpan.Zero;
                    socket.Bind(synodConfigProvider.LocalNode.GetServiceAddress());

                    while (!tokenSource.Token.IsCancellationRequested)
                    {
                        try
                        {
                            var request = socket.ReceiveMessage(config.ReceiveWaitTimeout);

                            if (request != null && !request.IsEmpty /*&& request.IsComplete*/)
                            {
                                logger.InfoFormat("Client message received by {0}", synodConfigProvider.LocalProcess.Id);

                                var response = ProcessRequest(request);

                                socket.SendMessage(new NetMQMessage(response.Frames));
                            }
                        }
                        catch (Exception err)
                        {
                            logger.Error(err);
                        }
                    }
                }
            }
            catch (Exception err)
            {
                logger.InfoFormat("Listening thread terminated! {0}", err);
            }
        }

        private IEnumerable<Thread> CreateRequestProcessingThreads()
        {
            for (var i = 0; i < config.ParallelMessageProcessors; i++)
            {
                var thread = new Thread(() => AcceptIncomingRequests(tokenSource.Token, context.CreateSocket(ZmqSocketType.Rep)));
                thread.Start();

                yield return thread;
            }

            device.Start();
        }

        private void AcceptIncomingRequests(CancellationToken token, NetMQSocket receiver)
        {
            using (receiver)
            {
                receiver.Options.SendHighWatermark = 100;
                receiver.Options.ReceiveHighWatermark = 200;
                receiver.Options.Linger = TimeSpan.Zero;
                receiver.Connect(InprocWorkersAddress);

                while (!token.IsCancellationRequested)
                {
                    try
                    {
                        var request = receiver.ReceiveMessage(config.ReceiveWaitTimeout);

                        if (request != null && !request.IsEmpty /*&& request.IsComplete*/)
                        {
                            var response = ProcessRequest(request);

                            receiver.SendMessage(new NetMQMessage(response.Frames));
                        }
                    }
                    catch (Exception err)
                    {
                        logger.Error(err);
                    }
                }
            }
        }

        private ClientMultipartMessage ProcessRequest(NetMQMessage request)
        {
            try
            {
                if (!LocalNodeIsActive())
                {
                    return CreatePassiveNodeErrorMessage();
                }

                return ProcessClientRequest(request);
            }
            catch (Exception err)
            {
                logger.Error(err);

                return new ClientMultipartMessage(new ErrorMessage(new Process {Id = synodConfigProvider.LocalProcess.Id},
                                                                   new ErrorMessage.Payload
                                                                   {
                                                                       NodeAddress = synodConfigProvider.LocalNode.BaseAddress,
                                                                       Error = err.ToString()
                                                                   }));
            }
        }

        private ClientMultipartMessage ProcessClientRequest(NetMQMessage request)
        {
            var multipartMessage = new ClientMultipartMessage(request);
            var message = new Message(new Envelope {Sender = new Process {Id = multipartMessage.GetSenderId()}},
                                      new Body
                                      {
                                          MessageType = multipartMessage.GetMessageType(),
                                          Content = multipartMessage.GetMessage()
                                      });

            var reply = PassClientRequestForProcessing(message);

            return new ClientMultipartMessage(reply);
        }

        private ClientMultipartMessage CreatePassiveNodeErrorMessage()
        {
            var localProcess = synodConfigProvider.LocalProcess;

            var errorMessage = new ErrorMessage(new Process {Id = localProcess.Id},
                                                new ErrorMessage.Payload
                                                {
                                                    ErrorCode = ErrorMessageCodes.NodeIsPassive,
                                                    NodeAddress = synodConfigProvider.LocalNode.GetServiceAddress()
                                                });
            return new ClientMultipartMessage(errorMessage);
        }

        private QueueDevice CreateProcessingDevice()
        {
            var queue = new QueueDevice(context,
                                        synodConfigProvider.LocalNode.GetServiceAddress(),
                                        InprocWorkersAddress);
            queue.Start();

            return queue;
        }

        private bool LocalNodeIsActive()
        {
            return synodConfigProvider.IsMemberOfSynod(synodConfigProvider.LocalNode);
        }

        public void Dispose()
        {
            try
            {
                if (!disposed)
                {
                    device.Stop();
                    tokenSource.Cancel(false);
                    processingThreads.ForEach(th => th.Join(config.ReceiveWaitTimeout));
                    tokenSource.Dispose();

                    disposed = true;
                }
            }
            catch (Exception err)
            {
                logger.Error(err);
            }
        }

        public void RegisterMessageProcessor(Func<IMessage, IMessage> handler)
        {
            messageHandler = handler;
        }

        private IMessage PassClientRequestForProcessing(IMessage request)
        {
            if (messageHandler != null)
            {
                return messageHandler(request);
            }

            return CreateNotProcessedErrorMessage();
        }

        private IMessage CreateNotProcessedErrorMessage()
        {
            var localProcess = synodConfigProvider.LocalProcess;

            return new ErrorMessage(new Process {Id = localProcess.Id},
                                    new ErrorMessage.Payload
                                    {
                                        ErrorCode = ErrorMessageCodes.MessageNotProcessed,
                                        NodeAddress = synodConfigProvider.LocalNode.GetServiceAddress()
                                    });
        }
    }
}