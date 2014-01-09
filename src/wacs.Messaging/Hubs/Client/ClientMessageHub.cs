using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using Castle.Core.Internal;
using wacs.Configuration;
using wacs.Diagnostics;
using wacs.Messaging.Hubs.Intercom;
using wacs.Messaging.Messages;
using wacs.Messaging.Messages.Client.Error;
using ZeroMQ;
using ZeroMQ.Devices;

namespace wacs.Messaging.Hubs.Client
{
    public class ClientMessageHub : IClientMessageHub
    {
        private const string InprocWorkersAddress = "inproc://processors";
        private Func<IMessage, IMessage> messageHandler;
        private readonly IClientMessageRouter messageRouter;
        private readonly ISynodConfigurationProvider synodConfigProvider;
        private bool disposed;
        private readonly QueueDevice device;
        private readonly ZmqContext context;
        private readonly CancellationTokenSource tokenSource;
        private readonly IClientMessageHubConfiguration config;
        private readonly IEnumerable<Thread> processingThreads;
        private readonly ILogger logger;

        public ClientMessageHub(ISynodConfigurationProvider synodConfigProvider,
                                IClientMessageRouter messageRouter,
                                IClientMessageHubConfiguration config,
                                ILogger logger)
        {
            this.logger = logger;
            tokenSource = new CancellationTokenSource();
            this.synodConfigProvider = synodConfigProvider;
            this.config = config;
            this.synodConfigProvider.SynodChanged += OnSynodChanged;
            this.messageRouter = messageRouter;
            context = ZmqContext.Create();
            device = CreateProcessingDevice();
            processingThreads = CreateRequestProcessingThreads().ToArray();
            device.Start();
        }

        private IEnumerable<Thread> CreateRequestProcessingThreads()
        {
            for (var i = 0; i < config.ParallelMessageProcessors; i++)
            {
                var thread = new Thread(() => AcceptIncomingRequests(tokenSource.Token, context.CreateSocket(SocketType.REP)));
                thread.Start();

                yield return thread;
            }
        }

        private void AcceptIncomingRequests(CancellationToken token, ZmqSocket receiver)
        {
            using (receiver)
            {
                receiver.Connect(InprocWorkersAddress);

                while (!token.IsCancellationRequested)
                {
                    var request = receiver.ReceiveMessage(config.ReceiveWaitTimeout);

                    var response = ProcessRequest(request);

                    receiver.SendMessage(new ZmqMessage(response.Frames));
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
            var multipartMessage = new MultipartMessage(request);
            var message = new Message(new Envelope {Sender = new Process(multipartMessage.GetSenderId())},
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

            var errorMessage = new ErrorMessage(localProcess,
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

        private void OnSynodChanged()
        {
            //if (LocalNodeIsActive(synodConfigProvider))
            //{
            //    socket.Bind(synodConfigProvider.LocalNode.GetServiceAddress());
            //}
            //else
            //{
            //    socket.Unbind(synodConfigProvider.LocalNode.GetServiceAddress());
            //}
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

            return new ErrorMessage(localProcess,
                                    new ErrorMessage.Payload
                                    {
                                        ErrorCode = ErrorMessageCodes.MessageNotProcessed,
                                        NodeAddress = synodConfigProvider.LocalNode.GetServiceAddress(),
                                        ProcessId = localProcess.Id
                                    });
        }
    }
}