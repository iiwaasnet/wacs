using System;
using System.Collections.Generic;
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

            var world = new[] {senderNode, receiverNode, remoteNode};

            var receiverObserver = new Mock<IObserver<IMessage>>();
            var remoteObserver = new Mock<IObserver<IMessage>>();


            using (var senderHub = CreateMessageHub(senderNode, senderProcess, world))
            {
                using (var receiverHub = CreateMessageHub(receiverNode, receiverProcess, world))
                {
                    using (var remoteHub = CreateMessageHub(remoteNode, remoteProcess, world))
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

        private static IIntercomMessageHub CreateMessageHub(INode node, IProcess process, IEnumerable<INode> world)
        {
            var logger = new Mock<ILogger>();

            var synodConfigProvider = new Mock<ISynodConfigurationProvider>();
            synodConfigProvider.Setup(m => m.LocalProcess).Returns(process);
            synodConfigProvider.Setup(m => m.LocalNode).Returns(node);
            synodConfigProvider.Setup(m => m.Synod).Returns(world);
            synodConfigProvider.Setup(m => m.World).Returns(world);

            return new IntercomMessageHub(synodConfigProvider.Object, logger.Object);
        }
    }
}