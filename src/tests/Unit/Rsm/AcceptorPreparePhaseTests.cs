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
    public class AcceptorPreparePhaseTests
    {
        [Test]
        public void OnPrepareRequestCameNotFromLeader_AcceptorRespondsWith_RsmNackPrepareNotLeader()
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
                SendPrepareMessage(proposerId, proposedLogIndex, proposalNumber, listener);
                Thread.Sleep(TimeSpan.FromSeconds(1));
            }

            messageHub.Verify(m => m.Send(It.IsAny<IProcess>(), It.Is<IMessage>(msg => typeof (RsmNackPrepareNotLeader) == msg.GetType())),
                              Times.Exactly(1));
        }

        [Test]
        public void OnPrepareRequestForChosenEntry_AcceptorRespondsWith_RsmNackPrepareChosen()
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
                SendPrepareMessage(leaderId, proposedLogIndex, proposalNumber, listener);
                Thread.Sleep(TimeSpan.FromSeconds(1));
            }

            messageHub.Verify(m => m.Send(It.IsAny<IProcess>(), It.Is<IMessage>(msg => typeof (RsmNackPrepareChosen) == msg.GetType())),
                              Times.Exactly(1));
        }

        [Test]
        public void OnPrepareRequestWithHigherBallotForAcceptedEntry_AcceptorRespondsWith_RsmAckPrepare()
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
            var syncCommand = new Mock<ISyncCommand>();
            var request = new Mock<IMessage>();
            request.Setup(m => m.Envelope).Returns(new Envelope());
            request.Setup(m => m.Body).Returns(new Body());
            syncCommand.Setup(m => m.Request).Returns(request.Object);
            replicatedLog.Setup(m => m.GetLogEntry(It.Is<ILogIndex>(l => l.Index == proposedLogIndex)))
                         .Returns(new LogEntry(syncCommand.Object, new LogIndex(proposedLogIndex), LogEntryState.Accepted));

            using (var acceptor = new Acceptor(messageHub.Object, replicatedLog.Object, leaseProvider.Object, nodeResolver.Object, logger.Object))
            {
                SendPrepareMessage(leaderId, proposedLogIndex, proposalNumber, listener);
                Thread.Sleep(TimeSpan.FromSeconds(1));
            }

            messageHub.Verify(m => m.Send(It.IsAny<IProcess>(), It.Is<IMessage>(msg => typeof (RsmAckPrepare) == msg.GetType())),
                              Times.Exactly(1));
        }

        [Test]
        public void OnPrepareRequestWithBallotThatEqualsMinAcceptorBallot_AcceptorRespondsWith_RsmNackPrepareBlocked()
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
            var syncCommand = new Mock<ISyncCommand>();
            var request = new Mock<IMessage>();
            request.Setup(m => m.Envelope).Returns(new Envelope());
            request.Setup(m => m.Body).Returns(new Body());
            syncCommand.Setup(m => m.Request).Returns(request.Object);
            replicatedLog.Setup(m => m.GetLogEntry(It.Is<ILogIndex>(l => l.Index == proposedLogIndex)))
                         .Returns(new LogEntry(syncCommand.Object, new LogIndex(proposedLogIndex), LogEntryState.Accepted));

            using (var acceptor = new Acceptor(messageHub.Object, replicatedLog.Object, leaseProvider.Object, nodeResolver.Object, logger.Object))
            {
                SendPrepareMessage(leaderId, proposedLogIndex, proposalNumber, listener);
                Thread.Sleep(TimeSpan.FromSeconds(1));
                messageHub.Verify(m => m.Send(It.IsAny<IProcess>(), It.Is<IMessage>(msg => typeof (RsmAckPrepare) == msg.GetType())),
                                  Times.Exactly(1));

                SendPrepareMessage(leaderId, proposedLogIndex, proposalNumber, listener);
                Thread.Sleep(TimeSpan.FromSeconds(1));
            }

            messageHub.Verify(m => m.Send(It.IsAny<IProcess>(), It.Is<IMessage>(msg => typeof (RsmNackPrepareBlocked) == msg.GetType())),
                              Times.Exactly(1));
        }

        [Test]
        public void OnPrepareRequestWithBallotThatLessThanMinAcceptorBallot_AcceptorRespondsWith_RsmNackPrepareBlocked()
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
            var syncCommand = new Mock<ISyncCommand>();
            var request = new Mock<IMessage>();
            request.Setup(m => m.Envelope).Returns(new Envelope());
            request.Setup(m => m.Body).Returns(new Body());
            syncCommand.Setup(m => m.Request).Returns(request.Object);
            replicatedLog.Setup(m => m.GetLogEntry(It.Is<ILogIndex>(l => l.Index == proposedLogIndex)))
                         .Returns(new LogEntry(syncCommand.Object, new LogIndex(proposedLogIndex), LogEntryState.Accepted));

            using (var acceptor = new Acceptor(messageHub.Object, replicatedLog.Object, leaseProvider.Object, nodeResolver.Object, logger.Object))
            {
                SendPrepareMessage(leaderId, proposedLogIndex, proposalNumber, listener);
                Thread.Sleep(TimeSpan.FromSeconds(1));
                messageHub.Verify(m => m.Send(It.IsAny<IProcess>(), It.Is<IMessage>(msg => typeof (RsmAckPrepare) == msg.GetType())),
                                  Times.Exactly(1));

                SendPrepareMessage(leaderId, proposedLogIndex, proposalNumber - 1, listener);
                Thread.Sleep(TimeSpan.FromSeconds(1));
            }

            messageHub.Verify(m => m.Send(It.IsAny<IProcess>(), It.Is<IMessage>(msg => typeof (RsmNackPrepareBlocked) == msg.GetType())),
                              Times.Exactly(1));
        }

        private static void SendPrepareMessage(int leaderId, ulong logIndex, ulong proposalNumber, Listener listener)
        {
            var rsmPrepare = new RsmPrepare(new wacs.Messaging.Messages.Process {Id = leaderId},
                                            new RsmPrepare.Payload
                                            {
                                                Leader = new wacs.Messaging.Messages.Process {Id = leaderId},
                                                LogIndex = new wacs.Messaging.Messages.Intercom.Rsm.LogIndex {Index = logIndex},
                                                Proposal = new Ballot {ProposalNumber = proposalNumber}
                                            });

            listener.Notify(rsmPrepare);
        }
    }
}