using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using wacs.Configuration;
using wacs.FLease;
using wacs.Framework;
using wacs.Messaging.Hubs.Intercom;
using wacs.Messaging.Messages;
using wacs.Messaging.Messages.Intercom.Rsm;
using wacs.Resolver;
using wacs.Rsm.Interface;
using Process = wacs.Messaging.Messages.Intercom.Process;

namespace wacs.Rsm.Implementation
{
    public class Consensus : IConsensus
    {
        private readonly IConsensusRoundManager consensusRoundManager;
        private readonly IIntercomMessageHub intercomMessageHub;
        private readonly IListener listener;
        private readonly IObservable<IMessage> ackPrepareStream;
        private readonly IObservable<IMessage> nackPrepareStream;
        private readonly ISynodConfigurationProvider synodConfigurationProvider;
        private readonly INodeResolver nodeResolver;
        private readonly IRsmConfiguration rsmConfig;

        public Consensus(IConsensusRoundManager consensusRoundManager,
                         IIntercomMessageHub intercomMessageHub,
                         ISynodConfigurationProvider synodConfigurationProvider,
                         INodeResolver nodeResolver,
                         IRsmConfiguration rsmConfig)
        {
            this.consensusRoundManager = consensusRoundManager;
            this.intercomMessageHub = intercomMessageHub;
            this.synodConfigurationProvider = synodConfigurationProvider;
            this.nodeResolver = nodeResolver;
            this.rsmConfig = rsmConfig;
            listener = intercomMessageHub.Subscribe();

            ackPrepareStream = listener.Where(m => m.Body.MessageType == RsmAckPrepare.MessageType);
            nackPrepareStream = listener.Where(m => m.Body.MessageType == RsmNackPrepareBlocked.MessageType
                                                    || m.Body.MessageType == RsmNackPrepareChosen.MessageType);
        }

        public IConsensusDecision Decide(ILogIndex index, IMessage command, bool fast)
        {
            var ballot = consensusRoundManager.GetNextBallot();
            var prepareOutcome = SendPrepare(index, ballot);

            if (prepareOutcome.Outcome == PreparePhaseOutcome.FailedDueToChosenLogEntry)
            {
                return new ConsensusDecision {Outcome = ConsensusOutcome.RejectedDueToChosenLogEntry};
            }

            return null;
        }

        private PreparePhaseResult SendPrepare(ILogIndex logIndex, Interface.IBallot ballot)
        {
            var ackFilter = new RsmPrepareAckMessageFilter(ballot, logIndex, nodeResolver, synodConfigurationProvider);
            var nackFilter = new RsmPrepareNackMessageFilter(ballot, logIndex, nodeResolver, synodConfigurationProvider);

            var awaitableAckFilter = new AwaitableMessageStreamFilter(ackFilter.Match, GetQuorum());
            var awaitableNackFilter = new AwaitableMessageStreamFilter(nackFilter.Match, GetQuorum());

            using (ackPrepareStream.Subscribe(awaitableAckFilter))
            {
                using (nackPrepareStream.Subscribe(awaitableNackFilter))
                {
                    var message = CreatePrepareMessage(logIndex, ballot);
                    intercomMessageHub.Broadcast(message);

                    var index = WaitHandle.WaitAny(new[] {awaitableAckFilter.Filtered, awaitableNackFilter.Filtered},
                                                   rsmConfig.CommandExecutionTimeout);

                    AssertPrepareTimeout(index);

                    if (PrepareAcknowledged(index))
                    {
                        return CreatePrepareSucceededResult(awaitableAckFilter.MessageStream);
                    }
                    if (PrepareRejected(index))
                    {
                        return CreatePrepareRejectedResult(awaitableNackFilter.MessageStream);
                    }

                    throw new InvalidStateException();
                }
            }
        }

        private PreparePhaseResult CreatePrepareRejectedResult(IEnumerable<IMessage> prepareResponses)
        {
            var alreadyChosen = prepareResponses.Where(m => m.Body.MessageType == RsmNackPrepareChosen.MessageType)
                                                .Select(m => new RsmNackPrepareChosen(m).GetPayload())
                                                .FirstOrDefault();
            if (alreadyChosen != null)
            {
                return new PreparePhaseResult
                       {
                           Outcome = PreparePhaseOutcome.FailedDueToChosenLogEntry
                       };
            }

            var acceptedBallot = prepareResponses.Where(m => m.Body.MessageType == RsmNackPrepareBlocked.MessageType)
                                                 .Select(m => new RsmNackPrepareBlocked(m).GetPayload())
                                                 .Max(p => p.AcceptedBallot);
            if (acceptedBallot != null)
            {
                return new PreparePhaseResult
                       {
                           Outcome = PreparePhaseOutcome.FailedDueToLowBallot,
                           AcceptedBallot = new Ballot(acceptedBallot.ProposalNumber)
                       };
            }

            throw new InvalidStateException();
        }

        private PreparePhaseResult CreatePrepareSucceededResult(IEnumerable<IMessage> prepareResponses)
        {
            var payloads = prepareResponses.Select(m => new RsmAckPrepare(m).GetPayload());
            var maxAcceptedBallot = payloads.Where(a => a.AcceptedValue != null)
                                            .Max(p => p.AcceptedBallot);
            if (maxAcceptedBallot != null)
            {
                return new PreparePhaseResult
                       {
                           AcceptedValue = payloads.First(p => p.AcceptedBallot.Equals(maxAcceptedBallot)).AcceptedValue,
                           Outcome = PreparePhaseOutcome.SucceededWithOtherValue
                       };
            }

            return new PreparePhaseResult {Outcome = PreparePhaseOutcome.SucceededWithProposedValue};
        }

        private bool PrepareRejected(int index)
        {
            return index == 1;
        }

        private bool PrepareAcknowledged(int index)
        {
            return index == 0;
        }

        private void AssertPrepareTimeout(int index)
        {
            if (index == WaitHandle.WaitTimeout)
            {
                throw new TimeoutException();
            }
        }

        private IMessage CreatePrepareMessage(ILogIndex index, Interface.IBallot ballot)
        {
            return new RsmPrepare(synodConfigurationProvider.LocalProcess,
                                  new RsmPrepare.Payload
                                  {
                                      Ballot = new Messaging.Messages.Intercom.Rsm.Ballot {ProposalNumber = ballot.ProposalNumber},
                                      LogIndex = new LogIndex {Index = index.Index},
                                      Leader = new Process {Id = synodConfigurationProvider.LocalProcess.Id}
                                  });
        }

        private int GetQuorum()
        {
            return synodConfigurationProvider.Synod.Count() / 2 + 1;
        }
    }
}