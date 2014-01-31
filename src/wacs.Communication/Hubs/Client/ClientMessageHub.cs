using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Castle.Core.Internal;
using wacs.Configuration;
using wacs.Diagnostics;
using wacs.Messaging.Messages;
using wacs.Messaging.Messages.Client.Error;
using ZeroMQ;
using ZeroMQ.Devices;
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
        private readonly ZmqContext context;
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
            context = ZmqContext.Create();
            new Thread(AcceptClientRequests).Start();
            //device = CreateProcessingDevice();
            //processingThreads = CreateRequestProcessingThreads().ToArray();
        }

        private void AcceptClientRequests()
        {
            try
            {
                using (var socket = context.CreateSocket(SocketType.REP))
                {
                    socket.SendHighWatermark = 100;
                    socket.ReceiveHighWatermark = 200;
                    socket.Bind(synodConfigProvider.LocalNode.GetServiceAddress());

                    while (!tokenSource.Token.IsCancellationRequested)
                    {
                        try
                        {
                            var request = socket.ReceiveMessage();
                            //var request = socket.ReceiveMessage(config.ReceiveWaitTimeout);

                            if (!request.IsEmpty && request.IsComplete)
                            {
                                var response = ProcessRequest(request);

                                socket.SendMessage(new ZmqMessage(response.Frames));
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
                var thread = new Thread(() => AcceptIncomingRequests(tokenSource.Token, context.CreateSocket(SocketType.REP)));
                thread.Start();

                yield return thread;
            }

            device.Start();
        }

        private void AcceptIncomingRequests(CancellationToken token, ZmqSocket receiver)
        {
            using (receiver)
            {
                receiver.SendHighWatermark = 100;
                receiver.ReceiveHighWatermark = 200;
                receiver.Connect(InprocWorkersAddress);

                while (!token.IsCancellationRequested)
                {
                    try
                    {
                        var request = receiver.ReceiveMessage(config.ReceiveWaitTimeout);

                        if (!request.IsEmpty && request.IsComplete)
                        {
                            var response = ProcessRequest(request);

                            receiver.SendMessage(new ZmqMessage(response.Frames));
                        }
                    }
                    catch (Exception err)
                    {
                        logger.Error(err);
                    }
                }
            }
        }

        private ClientMultipartMessage ProcessRequest(ZmqMessage request)
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

        private ClientMultipartMessage ProcessClientRequest(ZmqMessage request)
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
                                        InprocWorkersAddress,
                                        DeviceMode.Threaded);
            queue.Initialize();

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
                    device.Dispose();

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