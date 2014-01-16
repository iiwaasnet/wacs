using System;
using System.Collections.Concurrent;
using System.Threading;
using wacs.FLease;
using wacs.Framework.State;
using wacs.Messaging.Messages;
using wacs.Rsm.Interface;

namespace wacs.Rsm.Implementation
{
    public class Rsm : IRsm
    {
        private readonly BlockingCollection<IAwaitableResult<IMessage>> commandsQueue;
        private readonly IReplicatedLog replicatedLog;
        private readonly Thread processingThread;
        private readonly CancellationTokenSource cancellationSource;
        private readonly IConsensusFactory consensusFactory;
        private IConsensusDecision previousDecision;
        private readonly ILeaseProvider leaseProvider;

        public Rsm(IReplicatedLog replicatedLog, IConsensusFactory consensusFactory, ILeaseProvider leaseProvider)
        {
            commandsQueue = new BlockingCollection<IAwaitableResult<IMessage>>(new ConcurrentQueue<IAwaitableResult<IMessage>>());
            this.replicatedLog = replicatedLog;
            cancellationSource = new CancellationTokenSource();
            this.consensusFactory = consensusFactory;
            this.leaseProvider = leaseProvider;

            processingThread = new Thread(() => ProcessCommands(cancellationSource.Token));
            processingThread.Start();
        }

        private void ProcessCommands(CancellationToken token)
        {
            foreach (var awaitableResult in commandsQueue.GetConsumingEnumerable(token))
            {
                var command = awaitableResult;
                do
                {
                    command = ProcessCommand(command);
                } while (command != null);
            }
        }

        private IAwaitableResult<IMessage> ProcessCommand(IAwaitableResult<IMessage> awaitableResult)
        {
            var firstUnchosenLogEntry = replicatedLog.GetFirstUnchosenLogEntryIndex();
            var awaitable = (AwaitableRsmResponse) awaitableResult;
            var consensus = consensusFactory.CreateInstance();
            var decision = consensus.Decide(firstUnchosenLogEntry, awaitable.Command, RoundCouldBeFast());

            if (ConsensusNotReachedDueToShortHistoryPrefix(decision))
            {
                leaseProvider.ResetLease();
            }
            if (ConsensusReached(decision))
            {
                replicatedLog.SetLogEntryAccepted(firstUnchosenLogEntry,
                                          new LogEntry
                                          {
                                              State = LogEntryState.Chosen,
                                              Value = decision.DecidedValue
                                          });
            }

            if (decision.Outcome == ConsensusOutcome.DecidedWithProposedValue)
            {
                return null;
            }

            return awaitableResult;
        }

        private static bool ConsensusNotReachedDueToShortHistoryPrefix(IConsensusDecision decision)
        {
            return decision.Outcome == ConsensusOutcome.RejectedDueToChosenLogEntry
                   || decision.Outcome == ConsensusOutcome.FailedDueToNotBeingLeader;
        }

        private static bool ConsensusReached(IConsensusDecision decision)
        {
            return decision.Outcome == ConsensusOutcome.DecidedWithOtherValue
                   || decision.Outcome == ConsensusOutcome.DecidedWithProposedValue;
        }

        private bool RoundCouldBeFast()
        {
            return previousDecision != null && previousDecision.NextRoundCouldBeFast;
        }

        public IAwaitableResult<IMessage> EnqueueForExecution(IMessage command)
        {
            var awaitableRsmResponse = new AwaitableRsmResponse(command);
            commandsQueue.Add(awaitableRsmResponse);

            return awaitableRsmResponse;
        }

        void IDisposable.Dispose()
        {
            cancellationSource.Cancel(false);
            processingThread.Join();
            commandsQueue.Dispose();
            cancellationSource.Dispose();
        }
    }
}