using System;
using System.Threading;
using Moq;
using NUnit.Framework;
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
using LogIndex = wacs.Rsm.Implementation.LogIndex;
using Process = wacs.Configuration.Process;

namespace tests.Unit.Rsm
{
    [TestFixture]
    public class AcceptorAcceptPhaseTests
    {
        [Test]
        public void OnAcceptRequestCameNotFromLeader_AcceptorRespondsWith_RsmNackAcceptNotLeader()
        {
            var leaderId = 1;
            var proposerId = 2;
            var proposedLogIndex = 120UL;
            var proposalNumber = 100UL;

            var leaseProvider = new Mock<ILeaseProvider>();
            var lease = new Mock<ILease>();
            var leader = new Process(leaderId);
            lease.Setup(m => m.Owner).Returns(leader);
            leaseProvider.Setup(m => m.GetLease()).Returns(lease.Object);

            var messageHub = new Mock<IIntercomMessageHub>();
            var logger = new Mock<ILogger>();
            var listener = new Listener(s => { }, logger.Object);
            messageHub.Setup(c => c.Subscribe()).Returns(listener);

            var nodeResolver = new Mock<INodeResolver>();
            var acceptorProcess = new Process(12);
            nodeResolver.Setup(m => m.ResolveLocalNode()).Returns(acceptorProcess);

            using (var acceptor = new Acceptor(messageHub.Object, null, leaseProvider.Object, nodeResolver.Object, logger.Object))
            {
                SendAcceptMessage(proposerId, proposedLogIndex, proposalNumber, listener);
                Thread.Sleep(TimeSpan.FromSeconds(1));
            }

            messageHub.Verify(m => m.Send(It.IsAny<IProcess>(), It.Is<IMessage>(msg => typeof(RsmNackAcceptNotLeader) == msg.GetType())),
                              Times.Exactly(1));
        }

        [Test]
        public void OnAcceptRequestForChosenEntry_AcceptorRespondsWith_RsmNackAcceptChosen()
        {
            var leaderId = 1;
            var proposedLogIndex = 120UL;
            var proposalNumber = 100UL;

            var leaseProvider = new Mock<ILeaseProvider>();
            var lease = new Mock<ILease>();
            var leader = new Process(leaderId);
            lease.Setup(m => m.Owner).Returns(leader);
            leaseProvider.Setup(m => m.GetLease()).Returns(lease.Object);

            var messageHub = new Mock<IIntercomMessageHub>();
            var logger = new Mock<ILogger>();
            var listener = new Listener(s => { }, logger.Object);
            messageHub.Setup(c => c.Subscribe()).Returns(listener);

            var nodeResolver = new Mock<INodeResolver>();
            var acceptorProcess = new Process(12);
            nodeResolver.Setup(m => m.ResolveLocalNode()).Returns(acceptorProcess);

            var replicatedLog = new Mock<IReplicatedLog>();
            replicatedLog.Setup(m => m.GetLogEntry(It.Is<ILogIndex>(l => l.Index == proposedLogIndex)))
                         .Returns(new LogEntry(null, new LogIndex(proposedLogIndex), LogEntryState.Chosen));

            using (var acceptor = new Acceptor(messageHub.Object, replicatedLog.Object, leaseProvider.Object, nodeResolver.Object, logger.Object))
            {
                SendAcceptMessage(leaderId, proposedLogIndex, proposalNumber, listener);
                Thread.Sleep(TimeSpan.FromSeconds(1));
            }

            messageHub.Verify(m => m.Send(It.IsAny<IProcess>(), It.Is<IMessage>(msg => typeof(RsmNackAcceptChosen) == msg.GetType())),
                              Times.Exactly(1));
        }

        [Test]
        public void OnAcceptRequestWithBallotThatLessThanMinAcceptorBallot_AcceptorRespondsWith_RsmNackAcceptBlocked()
        {
            var leaderId = 1;
            var proposedLogIndex = 120UL;
            var proposalNumber = 100UL;

            var leaseProvider = new Mock<ILeaseProvider>();
            var lease = new Mock<ILease>();
            var leader = new Process(leaderId);
            lease.Setup(m => m.Owner).Returns(leader);
            leaseProvider.Setup(m => m.GetLease()).Returns(lease.Object);

            var messageHub = new Mock<IIntercomMessageHub>();
            var logger = new Mock<ILogger>();
            var listener = new Listener(s => { }, logger.Object);
            messageHub.Setup(c => c.Subscribe()).Returns(listener);

            var nodeResolver = new Mock<INodeResolver>();
            var acceptorProcess = new Process(12);
            nodeResolver.Setup(m => m.ResolveLocalNode()).Returns(acceptorProcess);

            var replicatedLog = new Mock<IReplicatedLog>();
            replicatedLog.Setup(m => m.GetLogEntry(It.Is<ILogIndex>(l => l.Index == proposedLogIndex)))
                         .Returns(new LogEntry(null, new LogIndex(proposedLogIndex), LogEntryState.Accepted));

            using (var acceptor = new Acceptor(messageHub.Object, replicatedLog.Object, leaseProvider.Object, nodeResolver.Object, logger.Object))
            {
                SendAcceptMessage(leaderId, proposedLogIndex, proposalNumber, listener);
                Thread.Sleep(TimeSpan.FromSeconds(1));
                messageHub.Verify(m => m.Send(It.IsAny<IProcess>(), It.Is<IMessage>(msg => typeof(RsmAckAccept) == msg.GetType())),
                                  Times.Exactly(1));

                SendAcceptMessage(leaderId, proposedLogIndex, proposalNumber - 1, listener);
                Thread.Sleep(TimeSpan.FromSeconds(1));
            }

            messageHub.Verify(m => m.Send(It.IsAny<IProcess>(), It.Is<IMessage>(msg => typeof(RsmNackAcceptBlocked) == msg.GetType())),
                              Times.Exactly(1));
        }

        private static void SendAcceptMessage(int leaderId, ulong logIndex, ulong proposalNumber, Listener listener)
        {
            var rsmAccept = new RsmAccept(new wacs.Messaging.Messages.Process {Id = leaderId},
                                          new RsmAccept.Payload
                                          {
                                              Leader = new wacs.Messaging.Messages.Process {Id = leaderId},
                                              LogIndex = new wacs.Messaging.Messages.Intercom.Rsm.LogIndex { Index = logIndex },
                                              Proposal = new Ballot{ProposalNumber = proposalNumber},
                                              Value = new Message(null, null)
                                          });
            listener.Notify(rsmAccept);
        }

    }
}