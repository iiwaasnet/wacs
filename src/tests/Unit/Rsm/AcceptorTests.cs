using System;
using System.Threading;
using Autofac;
using Moq;
using NUnit.Framework;
using tests.Unit.Helpers;
using wacs.Communication.Hubs.Intercom;
using wacs.Configuration;
using wacs.Diagnostics;
using wacs.FLease;
using wacs.Messaging.Messages;
using wacs.Messaging.Messages.Intercom.Rsm;
using wacs.Resolver;
using wacs.Rsm.Implementation;
using wacs.Rsm.Interface;
using Ballot = wacs.Messaging.Messages.Intercom.Rsm.Ballot;
using LogIndex = wacs.Messaging.Messages.Intercom.Rsm.LogIndex;
using Process = wacs.Messaging.Messages.Process;

namespace tests.Unit.Rsm
{
    [TestFixture]
    public class AcceptorTests
    {
        [Test]
        public void TestPrepareRequestCameNotFromLeader_RespondsWith_RsmNackPrepareNotLeader()
        {
            var leaderId = 1;
            var proposerId = 2;

            var leaseProvider = new Mock<ILeaseProvider>();
            var lease = new Mock<ILease>();
            var leader = new Mock<IProcess>();

            leader.Setup(m => m.Id).Returns(leaderId);
            lease.Setup(m => m.Owner).Returns(leader.Object);
            leaseProvider.Setup(m => m.GetLease()).Returns(lease.Object);

            var messageHub = new Mock<IIntercomMessageHub>();
            var logger = new Mock<ILogger>();
            var listener = new Listener(s => { }, logger.Object);
            messageHub.Setup(c => c.Subscribe()).Returns(listener);

            var nodeResolver = new Mock<INodeResolver>();
            var acceptorProcess = new wacs.Configuration.Process(12);
            nodeResolver.Setup(m => m.ResolveLocalNode()).Returns(acceptorProcess);

            using (var acceptor = new Acceptor(messageHub.Object, null, leaseProvider.Object, nodeResolver.Object, logger.Object))
            {
                listener.Notify(new RsmPrepare(new Process {Id = proposerId},
                                               new RsmPrepare.Payload
                                               {
                                                   Leader = new Process {Id = proposerId},
                                                   LogIndex = new LogIndex(),
                                                   Proposal = new Ballot()
                                               }));
                Thread.Sleep(TimeSpan.FromSeconds(1));
            }

            messageHub.Verify(m => m.Send(It.IsAny<IProcess>(), It.Is<IMessage>(msg => typeof (RsmNackPrepareNotLeader) == msg.GetType())),
                              Times.Exactly(1));
        }
        
        [Test]
        public void TestAcceptRequestCameNotFromLeader_RespondsWith_RsmNackAcceptNotLeader()
        {
            var leaderId = 1;
            var proposerId = 2;

            var leaseProvider = new Mock<ILeaseProvider>();
            var lease = new Mock<ILease>();
            var leader = new Mock<IProcess>();

            leader.Setup(m => m.Id).Returns(leaderId);
            lease.Setup(m => m.Owner).Returns(leader.Object);
            leaseProvider.Setup(m => m.GetLease()).Returns(lease.Object);

            var messageHub = new Mock<IIntercomMessageHub>();
            var logger = new Mock<ILogger>();
            var listener = new Listener(s => { }, logger.Object);
            messageHub.Setup(c => c.Subscribe()).Returns(listener);

            var nodeResolver = new Mock<INodeResolver>();
            var acceptorProcess = new wacs.Configuration.Process(12);
            nodeResolver.Setup(m => m.ResolveLocalNode()).Returns(acceptorProcess);

            using (var acceptor = new Acceptor(messageHub.Object, null, leaseProvider.Object, nodeResolver.Object, logger.Object))
            {
                listener.Notify(new RsmAccept(new Process {Id = proposerId},
                                               new RsmAccept.Payload
                                               {
                                                   Leader = new Process {Id = proposerId},
                                                   LogIndex = new LogIndex(),
                                                   Proposal = new Ballot(),
                                                   Value = new Message(null, null)
                                               }));
                Thread.Sleep(TimeSpan.FromSeconds(1));
            }

            messageHub.Verify(m => m.Send(It.IsAny<IProcess>(), It.Is<IMessage>(msg => typeof (RsmNackAcceptNotLeader) == msg.GetType())),
                              Times.Exactly(1));
        }
    }
}