using System;
using System.Linq;
using System.Threading;
using Moq;
using NUnit.Framework;
using tests.Unit.Helpers;
using wacs.Communication.Hubs.Intercom;
using wacs.Configuration;
using wacs.Diagnostics;
using wacs.Framework;
using wacs.Messaging.Messages;
using wacs.Messaging.Messages.Intercom.NodeResolver;
using wacs.Resolver;
using Node = wacs.Configuration.Node;
using Process = wacs.Configuration.Process;

namespace tests.Unit.Rsm
{
    public class NodeResolverTests
    {
        [Test]
        public void RegisteredNode_IsResolvedToProcess()
        {
            var logger = new Mock<ILogger>();

            var listener = new ListenerMock();
            var messageHub = new Mock<IIntercomMessageHub>();
            messageHub.Setup(m => m.Subscribe()).Returns(listener);

            var synodConfigProvider = new Mock<ISynodConfigurationProvider>();
            var localNode = new Node("127.0.0.1", 1255, 1266);
            var localProcess = new Process(2);
            synodConfigProvider.Setup(m => m.LocalNode).Returns(localNode);
            synodConfigProvider.Setup(m => m.LocalProcess).Returns(localProcess);
            var nodeResolverConfig = new Mock<INodeResolverConfiguration>();
            nodeResolverConfig.Setup(m => m.ProcessIdBroadcastPeriod).Returns(TimeSpan.FromSeconds(5));

            using (var nodeResolver = new NodeResolver(messageHub.Object, synodConfigProvider.Object, nodeResolverConfig.Object, logger.Object))
            {
                var remoteProcessId = 1;
                var baseAddress = "127.0.0.2";
                var intercomPort = 1244;
                var servicePort = 1255;

                SendProcessAnnouncementMessage(baseAddress, intercomPort, servicePort, remoteProcessId, listener);

                var process = nodeResolver.ResolveRemoteNode(new Node(baseAddress, intercomPort, servicePort));

                Assert.AreEqual(remoteProcessId, process.Id);
            }
        }

        [Test]
        public void ResolvingNotRegisteredNode_ReturnsNull()
        {
            var logger = new Mock<ILogger>();

            var listener = new ListenerMock();
            var messageHub = new Mock<IIntercomMessageHub>();
            messageHub.Setup(m => m.Subscribe()).Returns(listener);

            var synodConfigProvider = new Mock<ISynodConfigurationProvider>();
            var localNode = new Node("127.0.0.1", 1255, 1266);
            var localProcess = new Process(2);
            synodConfigProvider.Setup(m => m.LocalNode).Returns(localNode);
            synodConfigProvider.Setup(m => m.LocalProcess).Returns(localProcess);
            var nodeResolverConfig = new Mock<INodeResolverConfiguration>();
            nodeResolverConfig.Setup(m => m.ProcessIdBroadcastPeriod).Returns(TimeSpan.FromSeconds(5));

            using (var nodeResolver = new NodeResolver(messageHub.Object, synodConfigProvider.Object, nodeResolverConfig.Object, logger.Object))
            {
                var baseAddress = "127.0.0.2";
                var intercomPort = 1244;
                var servicePort = 1255;

                var process = nodeResolver.ResolveRemoteNode(new Node(baseAddress, intercomPort, servicePort));

                Assert.IsNull(process);
            }
        }

        [Test]
        public void FromTwoRemoteNodesWithSameProcessId_OnlyOneIsRegistered()
        {
            var localProcessId = 3;
            var logger = new Mock<ILogger>();

            var listener = new ListenerMock();
            var messageHub = new Mock<IIntercomMessageHub>();
            messageHub.Setup(m => m.Subscribe()).Returns(listener);

            var synodConfigProvider = new Mock<ISynodConfigurationProvider>();
            var localNode = new Node("127.0.0.1", 1255, 1266);
            var localProcess = new Process(localProcessId);
            synodConfigProvider.Setup(m => m.LocalNode).Returns(localNode);
            synodConfigProvider.Setup(m => m.LocalProcess).Returns(localProcess);
            var nodeResolverConfig = new Mock<INodeResolverConfiguration>();
            nodeResolverConfig.Setup(m => m.ProcessIdBroadcastPeriod).Returns(TimeSpan.FromSeconds(5));

            using (var nodeResolver = new NodeResolver(messageHub.Object, synodConfigProvider.Object, nodeResolverConfig.Object, logger.Object))
            {
                var baseAddresses = new[] {"127.0.0.2", "127.0.0.3"};
                var intercomPort = 1244;
                var servicePort = 1255;
                var processId = 1;

                SendProcessAnnouncementMessage(baseAddresses.First(), intercomPort, servicePort, processId, listener);
                var remoteNode1 = nodeResolver.ResolveRemoteProcess(new Process(processId));
                Assert.IsTrue(baseAddresses.Contains(remoteNode1.BaseAddress));

                SendProcessAnnouncementMessage(baseAddresses.Second(), intercomPort, servicePort, processId, listener);
                var remoteNode2 = nodeResolver.ResolveRemoteProcess(new Process(processId));
                Assert.AreEqual(remoteNode1.BaseAddress, remoteNode2.BaseAddress);

                logger.Verify(m => m.WarnFormat(It.IsAny<string>(), It.IsAny<object[]>()), Times.Once());
            }
        }

        [Test]
        public void NodeResolver_BroadcastsLocalNode()
        {
            var logger = new Mock<ILogger>();
            var broadcastPeriod = TimeSpan.FromMilliseconds(500);

            var listener = new Listener(u => { }, logger.Object);
            var messageHub = new Mock<IIntercomMessageHub>();
            messageHub.Setup(m => m.Subscribe()).Returns(listener);

            var synodConfigProvider = new Mock<ISynodConfigurationProvider>();
            var localNode = new Node("127.0.0.1", 1255, 1266);
            var localProcess = new Process(2);
            synodConfigProvider.Setup(m => m.LocalNode).Returns(localNode);
            synodConfigProvider.Setup(m => m.LocalProcess).Returns(localProcess);
            var nodeResolverConfig = new Mock<INodeResolverConfiguration>();
            nodeResolverConfig.Setup(m => m.ProcessIdBroadcastPeriod).Returns(broadcastPeriod);

            using (var nodeResolver = new NodeResolver(messageHub.Object, synodConfigProvider.Object, nodeResolverConfig.Object, logger.Object))
            {
                var broadcastTimes = 3;
                var wait = new TimeSpan(broadcastPeriod.Ticks * broadcastTimes);

                Thread.Sleep(wait);

                messageHub.Verify(m => m.Broadcast(It.Is<IMessage>(v => v.GetType() == typeof (ProcessAnnouncementMessage))), Times.AtLeast(broadcastTimes - 1));
            }
        }

        [Test]
        public void ChangingSynodConfigration_RemovesNonExistingNodes()
        {
            var logger = new Mock<ILogger>();

            var listener = new ListenerMock();
            var messageHub = new Mock<IIntercomMessageHub>();
            messageHub.Setup(m => m.Subscribe()).Returns(listener);

            var synodConfigProvider = new Mock<ISynodConfigurationProvider>();
            var localNode = new Node("127.0.0.1", 1255, 1266);
            var localProcess = new Process(2);
            synodConfigProvider.Setup(m => m.LocalNode).Returns(localNode);
            synodConfigProvider.Setup(m => m.LocalProcess).Returns(localProcess);
            synodConfigProvider.Setup(m => m.World).Returns(new[] {localNode});
            var nodeResolverConfig = new Mock<INodeResolverConfiguration>();
            nodeResolverConfig.Setup(m => m.ProcessIdBroadcastPeriod).Returns(TimeSpan.FromSeconds(5));

            using (var nodeResolver = new NodeResolver(messageHub.Object, synodConfigProvider.Object, nodeResolverConfig.Object, logger.Object))
            {
                var remoteProcess = 1;
                var baseAddress = "127.0.0.2";
                var intercomPort = 1244;
                var servicePort = 1255;

                SendProcessAnnouncementMessage(baseAddress, intercomPort, servicePort, remoteProcess, listener);

                var process = nodeResolver.ResolveRemoteNode(new Node(baseAddress, intercomPort, servicePort));
                Assert.AreEqual(remoteProcess, process.Id);

                synodConfigProvider.Raise(p => { p.WorldChanged += () => { }; });

                process = nodeResolver.ResolveRemoteNode(new Node(baseAddress, intercomPort, servicePort));
                Assert.IsNull(process);

                Assert.IsNotNull(nodeResolver.ResolveLocalNode());
            }
        }

        private static void SendProcessAnnouncementMessage(string baseAddress, int intercomPort, int servicePort, int senderId, ListenerMock listener)
        {
            var remoteNode = new wacs.Messaging.Messages.Intercom.NodeResolver.Node
                             {
                                 BaseAddress = baseAddress,
                                 IntercomPort = intercomPort,
                                 ServicePort = servicePort
                             };
            var remoteProcess = new wacs.Messaging.Messages.Process {Id = senderId};
            var msg = new ProcessAnnouncementMessage(remoteProcess,
                                                     new ProcessAnnouncementMessage.Payload
                                                     {
                                                         Process = remoteProcess,
                                                         Node = remoteNode
                                                     });
            listener.Notify(msg);
        }
    }
}