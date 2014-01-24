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
            device = CreateProcessingDevice();
            processingThreads = CreateRequestProcessingThreads().ToArray();
        }

        private IEnumerable<Thread> CreateRequestProcessingThreads()
        {
            for (var i = 0; i < config.ParallelMessageProcessors; i++)
            {
                var thread = new Thread(() => AcceptIncomingRequests(tokenSource.Token, context.CreateSocket(SocketType.REP)));
                thread.Start();

                yield return thread;
            }

            var deviceThread = new Thread(() => device.Start());
            deviceThread.Start();

            yield return deviceThread;
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
                    var request = receiver.ReceiveMessage(config.ReceiveWaitTimeout);

                    if (!request.IsEmpty)
                    {
                        var response = ProcessRequest(request);

                        receiver.SendMessage(new ZmqMessage(response.Frames));
                    }
                }
            }
        }

        private ClientMultipartMessage ProcessRequest(ZmqMessage request)
        {
            if (!LocalNodeIsActive())
            {
                return CreatePassiveNodeErrorMessage();
            }

            return ProcessClientRequest(request);
        }

        private ClientMultipartMessage ProcessClientRequest(ZmqMessage request)
        {
            var multipartMessage = new ClientMultipartMessage(request);
            var message = new Message(new Envelope {Sender = new Messaging.Messages.Process{Id = multipartMessage.GetSenderId()}},
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

            var errorMessage = new ErrorMessage(new Messaging.Messages.Process{Id = localProcess.Id},
                                                new ErrorMessage.Payload
                                                {
                                                    ErrorCode = ErrorMessageCodes.NodeIsPassive,
                                                    NodeAddress = synodConfigProvider.LocalNode.GetServiceAddress(),
                                                    ProcessId = localProcess.Id
                                                });
            return new ClientMultipartMessage(errorMessage);
        }

        private QueueDevice CreateProcessingDevice()
        {
            var queue = new QueueDevice(context,
                                        synodConfigProvider.LocalNode.GetServiceAddress(),
                                        InprocWorkersAddress,
                                        DeviceMode.Blocking);
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

            return new ErrorMessage(new Messaging.Messages.Process{Id = localProcess.Id},
                                    new ErrorMessage.Payload
                                    {
                                        ErrorCode = ErrorMessageCodes.MessageNotProcessed,
                                        NodeAddress = synodConfigProvider.LocalNode.GetServiceAddress(),
                                        ProcessId = localProcess.Id
                                    });
        }
    }
}