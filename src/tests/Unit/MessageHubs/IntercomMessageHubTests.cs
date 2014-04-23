using System;
using System.Threading;
using Moq;
using NUnit.Framework;
using wacs.Communication.Hubs.Intercom;
using wacs.Configuration;
using wacs.Diagnostics;
using wacs.Messaging.Messages;
using wacs.Messaging.Messages.Intercom.Lease;
using Process = wacs.Configuration.Process;

namespace tests.Unit.MessageHubs
{
    [TestFixture]
    public class IntercomMessageHubTests
    {
        [Test]
        public void MessageSendDirectlyToReceiver_ReceivedOnlyByReceiver()
        {
            var senderProcess = new Process(1);
            var receiverProcess = new Process(2);
            var remoteProcess = new Process(3);
            var senderPort = 2455;
            var receiverPort = 2456;
            var remotePort = 2457;
            var senderNode = new Node("tcp://127.0.0.1", senderPort, 2355);
            var receiverNode = new Node("tcp://127.0.0.1", receiverPort, 2356);
            var remoteNode = new Node("tcp://127.0.0.1", remotePort, 2356);

            var senderSynodConfigProvider = new Mock<ISynodConfigurationProvider>();
            senderSynodConfigProvider.Setup(m => m.LocalProcess).Returns(senderProcess);
            senderSynodConfigProvider.Setup(m => m.LocalNode).Returns(senderNode);
            senderSynodConfigProvider.Setup(m => m.Synod).Returns(new[] {senderNode, receiverNode, remoteNode});
            senderSynodConfigProvider.Setup(m => m.World).Returns(new[] {senderNode, receiverNode, remoteNode});

            var receiverSynodConfigProvider = new Mock<ISynodConfigurationProvider>();
            receiverSynodConfigProvider.Setup(m => m.LocalProcess).Returns(receiverProcess);
            receiverSynodConfigProvider.Setup(m => m.LocalNode).Returns(receiverNode);
            receiverSynodConfigProvider.Setup(m => m.Synod).Returns(new[] {senderNode, receiverNode, remoteNode});
            receiverSynodConfigProvider.Setup(m => m.World).Returns(new[] {senderNode, receiverNode, remoteNode});

            var remoteSynodConfigProvider = new Mock<ISynodConfigurationProvider>();
            remoteSynodConfigProvider.Setup(m => m.LocalProcess).Returns(remoteProcess);
            remoteSynodConfigProvider.Setup(m => m.LocalNode).Returns(remoteNode);
            remoteSynodConfigProvider.Setup(m => m.Synod).Returns(new[] {senderNode, receiverNode, remoteNode});
            remoteSynodConfigProvider.Setup(m => m.World).Returns(new[] {senderNode, receiverNode, remoteNode});

            var receiverObserver = new Mock<IObserver<IMessage>>();
            var remoteObserver = new Mock<IObserver<IMessage>>();

            var logger = new Mock<ILogger>();

            using (var senderHub = new IntercomMessageHub(senderSynodConfigProvider.Object, logger.Object))
            {
                using (var receiverHub = new IntercomMessageHub(receiverSynodConfigProvider.Object, logger.Object))
                {
                    using (var remoteHub = new IntercomMessageHub(remoteSynodConfigProvider.Object, logger.Object))
                    {
                        using (var remoteListener = remoteHub.Subscribe())
                        {
                            using (remoteListener.Subscribe(remoteObserver.Object))
                            {
                                remoteListener.Start();
                                using (var receiverListener = receiverHub.Subscribe())
                                {
                                    using (receiverListener.Subscribe(receiverObserver.Object))
                                    {
                                        receiverListener.Start();

                                        var message = new LeaseAckWrite(new wacs.Messaging.Messages.Process
                                                                        {
                                                                            Id = receiverProcess.Id
                                                                        },
                                                                        new LeaseAckWrite.Payload());
                                        SendMessage(senderHub, receiverProcess, message);

                                        receiverObserver.Verify(m => m.OnNext(It.Is<IMessage>(v => v.Body.MessageType == LeaseAckWrite.MessageType)), Times.Once());
                                        remoteObserver.Verify(m => m.OnNext(It.IsAny<IMessage>()), Times.Never);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        private static void SendMessage(IIntercomMessageHub hub, IProcess recipient, IMessage message)
        {
            hub.Send(recipient, message);

            WaitUntilMessageDelivered();
        }

        private static void WaitUntilMessageDelivered()
        {
            Thread.Sleep(TimeSpan.FromSeconds(1));
        }
    }
}