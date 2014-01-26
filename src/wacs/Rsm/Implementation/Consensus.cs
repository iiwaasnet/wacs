using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using wacs.Communication.Hubs.Intercom;
using wacs.Configuration;
using wacs.Diagnostics;
using wacs.FLease;
using wacs.Framework;
using wacs.Messaging.Messages;
using wacs.Messaging.Messages.Intercom.Rsm;
using wacs.Resolver;
using wacs.Rsm.Interface;
using IBallot = wacs.Rsm.Interface.IBallot;
using Process = wacs.Messaging.Messages.Process;

namespace wacs.Rsm.Implementation
{
    public class Consensus : IConsensus
    {
        private readonly IConsensusRoundManager consensusRoundManager;
        private readonly IIntercomMessageHub intercomMessageHub;
        private readonly IListener listener;
        private readonly IObservable<IMessage> ackPrepareStream;
        private readonly IObservable<IMessage> ackAcceptStream;
        private readonly IObservable<IMessage> nackPrepareStream;
        private readonly IObservable<IMessage> nackAcceptStream;
        private readonly ISynodConfigurationProvider synodConfigurationProvider;
        private readonly INodeResolver nodeResolver;
        private readonly IRsmConfiguration rsmConfig;
        private readonly IReplicatedLog replicatedLog;
        private readonly ILogger logger;

        public Consensus(IConsensusRoundManager consensusRoundManager,
                         IIntercomMessageHub intercomMessageHub,
                         ISynodConfigurationProvider synodConfigurationProvider,
                         IReplicatedLog replicatedLog,
                         INodeResolver nodeResolver,
                         IRsmConfiguration rsmConfig,
                         ILogger logger)
        {
            this.logger = logger;
            this.consensusRoundManager = consensusRoundManager;
            this.intercomMessageHub = intercomMessageHub;
            this.synodConfigurationProvider = synodConfigurationProvider;
            this.nodeResolver = nodeResolver;
            this.rsmConfig = rsmConfig;
            this.replicatedLog = replicatedLog;
            listener = intercomMessageHub.Subscribe();

            ackPrepareStream = listener.Where(m => m.Body.MessageType == RsmAckPrepare.MessageType);
            nackPrepareStream = listener.Where(m => m.Body.MessageType == RsmNackPrepareBlocked.MessageType
                                                    || m.Body.MessageType == RsmNackPrepareChosen.MessageType
                                                    || m.Body.MessageType == RsmNackPrepareNotLeader.MessageType);

            ackAcceptStream = listener.Where(m => m.Body.MessageType == RsmAckAccept.MessageType);
            nackAcceptStream = listener.Where(m => m.Body.MessageType == RsmNackAcceptBlocked.MessageType
                                                   || m.Body.MessageType == RsmNackAcceptChosen.MessageType
                                                   || m.Body.MessageType == RsmNackAcceptNotLeader.MessageType);

            listener.Start();
        }

        public IConsensusDecision Decide(ILogIndex logIndex, ISyncCommand command, bool fast)
        {
            var ballot = consensusRoundManager.GetNextBallot();

            var timer = new Stopwatch();
            timer.Start();

            var preparePhase = RunPreparePhase(ballot, logIndex);
            if (PrepareNotCommitted(preparePhase))
            {
                return CreateValueNotDecidedResponse(preparePhase);
            }

            timer.Stop();
            logger.InfoFormat("Prepare phase finished in {0} msec", timer.ElapsedMilliseconds);
            timer.Restart();

            var value = (preparePhase.Outcome == PreparePhaseOutcome.SucceededWithOtherValue)
                            ? preparePhase.AcceptedValue
                            : command.Request;

            var acceptPhase = RunAcceptPhase(ballot, logIndex, value);

            var decision = CreateConsensusDecision(acceptPhase, preparePhase, command.Request);

            timer.Stop();
            logger.InfoFormat("Accept phase finished in {0} msec", timer.ElapsedMilliseconds);
            timer.Restart();

            if (ConsensusReached(decision))
            {
                var chosenValue = (AwaitableRsmRequest) command;
                chosenValue.Request = decision.DecidedValue;
                replicatedLog.SetLogEntryChosen(logIndex, chosenValue);

                BroadcastChosenValue(logIndex, decision.DecidedValue);
            }


            timer.Stop();
            logger.InfoFormat("Broadcast chosen value in {0} msec", timer.ElapsedMilliseconds);

            return decision;
        }

        private void BroadcastChosenValue(ILogIndex logIndex, IMessage chosenValue)
        {
            var message = CreateChosenMessage(logIndex, chosenValue);
            intercomMessageHub.Broadcast(message);
        }

        private IMessage CreateChosenMessage(ILogIndex logIndex, IMessage chosenValue)
        {
            return new RsmChosen(new Process {Id = synodConfigurationProvider.LocalProcess.Id},
                                 new RsmChosen.Payload
                                 {
                                     LogIndex = new Messaging.Messages.Intercom.Rsm.LogIndex {Index = logIndex.Index},
                                     Leader = new Process {Id = synodConfigurationProvider.LocalProcess.Id},
                                     Value = new Message(chosenValue.Envelope, chosenValue.Body)
                                 });
        }

        private static bool ConsensusReached(IConsensusDecision decision)
        {
            return decision.Outcome == ConsensusOutcome.DecidedWithOtherValue
                   || decision.Outcome == ConsensusOutcome.DecidedWithProposedValue;
        }

        private IConsensusDecision CreateConsensusDecision(AcceptPhaseResult acceptPhase, PreparePhaseResult preparePhase, IMessage proposedValue)
        {
            if (acceptPhase.Outcome == AcceptPhaseOutcome.FailedDueToLowBallot)
            {
                return new ConsensusDecision {Outcome = ConsensusOutcome.FailedDueToLowBallot};
            }

            return new ConsensusDecision
                   {
                       Outcome = (preparePhase.Outcome == PreparePhaseOutcome.SucceededWithOtherValue)
                                     ? ConsensusOutcome.DecidedWithOtherValue
                                     : ConsensusOutcome.DecidedWithProposedValue,
                       DecidedValue = (preparePhase.Outcome == PreparePhaseOutcome.SucceededWithOtherValue)
                                          ? preparePhase.AcceptedValue
                                          : proposedValue
                   };

            throw new InvalidStateException();
        }

        private AcceptPhaseResult RunAcceptPhase(IBallot ballot, ILogIndex index, IMessage command)
        {
            return SendAccept(ballot, index, command);
        }

        private AcceptPhaseResult SendAccept(IBallot ballot, ILogIndex logIndex, IMessage value)
        {
            var ackFilter = new RsmMessageFilter(ballot, logIndex, (m) => new RsmAckAccept(m).GetPayload(), nodeResolver, synodConfigurationProvider);
            var nackFilter = new RsmAcceptNackMessageFilter(ballot, logIndex, nodeResolver, synodConfigurationProvider);

            var awaitableAckFilter = new AwaitableMessageStreamFilter(ackFilter.Match, GetQuorum());
            var awaitableNackFilter = new AwaitableMessageStreamFilter(nackFilter.Match, GetQuorum());

            using (ackAcceptStream.Subscribe(awaitableAckFilter))
            {
                using (nackAcceptStream.Subscribe(awaitableNackFilter))
                {
                    var message = CreateAcceptMessage(logIndex, ballot, value);

                    var timer = new Stopwatch();
                    timer.Start();


                    intercomMessageHub.Broadcast(message);

                    timer.Stop();
                    logger.InfoFormat("SendAccept {0} msec", timer.ElapsedMilliseconds);

                    var index = WaitHandle.WaitAny(new[] {awaitableAckFilter.Filtered, awaitableNackFilter.Filtered},
                                                   rsmConfig.CommandExecutionTimeout);

                    AssertPrepareTimeout(index);

                    if (PhaseAcknowledged(index))
                    {
                        return CreateAcceptSucceededResult();
                    }
                    if (PhaseRejected(index))
                    {
                        return CreateAcceptRejectedResult(awaitableNackFilter.MessageStream);
                    }

                    throw new InvalidStateException();
                }
            }
        }

        private AcceptPhaseResult CreateAcceptRejectedResult(IEnumerable<IMessage> messageStream)
        {
            var minProposal = messageStream.Select(m => new RsmNackAcceptBlocked(m).GetPayload())
                                           .Where(p => p.MinProposal != null)
                                           .Max(p => p.MinProposal);
            return new AcceptPhaseResult
                   {
                       Outcome = AcceptPhaseOutcome.FailedDueToLowBallot,
                       MinProposal = new Ballot(minProposal.ProposalNumber)
                   };
        }

        private AcceptPhaseResult CreateAcceptSucceededResult()
        {
            return new AcceptPhaseResult {Outcome = AcceptPhaseOutcome.Succeeded};
        }

        private IMessage CreateAcceptMessage(ILogIndex logIndex, IBallot ballot, IMessage value)
        {
            return new RsmAccept(new Process {Id = synodConfigurationProvider.LocalProcess.Id},
                                 new RsmAccept.Payload
                                 {
                                     Proposal = new Messaging.Messages.Intercom.Rsm.Ballot {ProposalNumber = ballot.ProposalNumber},
                                     LogIndex = new Messaging.Messages.Intercom.Rsm.LogIndex {Index = logIndex.Index},
                                     Leader = new Process {Id = synodConfigurationProvider.LocalProcess.Id},
                                     Value = new Message(value.Envelope, value.Body)
                                 });
        }

        private PreparePhaseResult RunPreparePhase(IBallot ballot, ILogIndex index)
        {
            var preparePhase = SendPrepare(index, ballot);

            if (preparePhase.Outcome == PreparePhaseOutcome.FailedDueToLowBallot)
            {
                consensusRoundManager.SetMinBallot(preparePhase.MinProposal);
            }

            return preparePhase;
        }

        private IConsensusDecision CreateValueNotDecidedResponse(PreparePhaseResult prepareOutcome)
        {
            switch (prepareOutcome.Outcome)
            {
                case PreparePhaseOutcome.FailedDueToChosenLogEntry:
                    return new ConsensusDecision {Outcome = ConsensusOutcome.RejectedDueToChosenLogEntry};
                case PreparePhaseOutcome.FailedDueToNotBeingLeader:
                    return new ConsensusDecision {Outcome = ConsensusOutcome.FailedDueToNotBeingLeader};
                case PreparePhaseOutcome.FailedDueToLowBallot:
                    return new ConsensusDecision {Outcome = ConsensusOutcome.FailedDueToLowBallot};
                default:
                    throw new Exception(string.Format("PreparePhaseOutcome {0} is not known!", prepareOutcome.Outcome));
            }
        }

        private bool PrepareNotCommitted(PreparePhaseResult prepareOutcome)
        {
            return prepareOutcome.Outcome != PreparePhaseOutcome.SucceededWithOtherValue
                   && prepareOutcome.Outcome != PreparePhaseOutcome.SucceededWithProposedValue;
        }

        private bool PrepareNotCommitted(IConsensusDecision decision)
        {
            return decision.Outcome != ConsensusOutcome.DecidedWithOtherValue
                   && decision.Outcome != ConsensusOutcome.DecidedWithProposedValue;
        }

        private PreparePhaseResult SendPrepare(ILogIndex logIndex, IBallot ballot)
        {
            var ackFilter = new RsmMessageFilter(ballot, logIndex, (m) => new RsmAckPrepare(m).GetPayload(), nodeResolver, synodConfigurationProvider);
            var nackFilter = new RsmPrepareNackMessageFilter(ballot, logIndex, nodeResolver, synodConfigurationProvider);

            var awaitableAckFilter = new AwaitableMessageStreamFilter(ackFilter.Match, GetQuorum());
            var awaitableNackFilter = new AwaitableMessageStreamFilter(nackFilter.Match, GetQuorum());

            using (ackPrepareStream.Subscribe(awaitableAckFilter))
            {
                using (nackPrepareStream.Subscribe(awaitableNackFilter))
                {
                    var message = CreatePrepareMessage(logIndex, ballot);

                    var timer = new Stopwatch();
                    timer.Start();

                    intercomMessageHub.Broadcast(message);

                    var index = WaitHandle.WaitAny(new[] {awaitableAckFilter.Filtered, awaitableNackFilter.Filtered},
                                                   rsmConfig.CommandExecutionTimeout);

                    timer.Stop();
                    logger.InfoFormat("SendPrepare {0} msec", timer.ElapsedMilliseconds);


                    AssertPrepareTimeout(index);

                    if (PhaseAcknowledged(index))
                    {
                        return CreatePrepareSucceededResult(awaitableAckFilter.MessageStream);
                    }
                    if (PhaseRejected(index))
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

            var minProposal = prepareResponses.Where(m => m.Body.MessageType == RsmNackPrepareBlocked.MessageType)
                                              .Select(m => new RsmNackPrepareBlocked(m).GetPayload())
                                              .Max(p => p.MinProposal);
            if (minProposal != null)
            {
                return new PreparePhaseResult
                       {
                           Outcome = PreparePhaseOutcome.FailedDueToLowBallot,
                           MinProposal = new Ballot(minProposal.ProposalNumber)
                       };
            }

            if (prepareResponses.Any(m => m.Body.MessageType == RsmNackPrepareNotLeader.MessageType))
            {
                return new PreparePhaseResult
                       {
                           Outcome = PreparePhaseOutcome.FailedDueToNotBeingLeader
                       };
            }

            throw new InvalidStateException();
        }

        private PreparePhaseResult CreatePrepareSucceededResult(IEnumerable<IMessage> prepareResponses)
        {
            var payloads = prepareResponses.Select(m => new RsmAckPrepare(m).GetPayload());
            var maxAcceptedProposal = payloads.Where(a => a.AcceptedValue != null && a.AcceptedProposal != null)
                                              .Max(p => p.AcceptedProposal);
            if (maxAcceptedProposal != null)
            {
                return new PreparePhaseResult
                       {
                           AcceptedValue = payloads.First(p => p.AcceptedProposal.Equals(maxAcceptedProposal)).AcceptedValue,
                           Outcome = PreparePhaseOutcome.SucceededWithOtherValue
                       };
            }

            return new PreparePhaseResult {Outcome = PreparePhaseOutcome.SucceededWithProposedValue};
        }

        private bool PhaseRejected(int index)
        {
            return index == 1;
        }

        private bool PhaseAcknowledged(int index)
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

        private IMessage CreatePrepareMessage(ILogIndex index, IBallot ballot)
        {
            return new RsmPrepare(new Process {Id = synodConfigurationProvider.LocalProcess.Id},
                                  new RsmPrepare.Payload
                                  {
                                      Proposal = new Messaging.Messages.Intercom.Rsm.Ballot {ProposalNumber = ballot.ProposalNumber},
                                      LogIndex = new Messaging.Messages.Intercom.Rsm.LogIndex {Index = index.Index},
                                      Leader = new Process {Id = synodConfigurationProvider.LocalProcess.Id}
                                  });
        }

        private int GetQuorum()
        {
            return synodConfigurationProvider.Synod.Count() / 2 + 1;
        }

        public void Dispose()
        {
            listener.Stop();
            listener.Dispose();
        }
    }
}