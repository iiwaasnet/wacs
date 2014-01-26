using System;
using System.Collections.Concurrent;
using System.Threading;
using wacs.Diagnostics;
using wacs.FLease;
using wacs.Framework.State;
using wacs.Messaging.Messages;
using wacs.Rsm.Interface;

namespace wacs.Rsm.Implementation
{
    public class Rsm : IRsm
    {
        private readonly BlockingCollection<IAwaitableResponse<IMessage>> commandsQueue;
        private readonly IReplicatedLog replicatedLog;
        private readonly Thread processingThread;
        private IConsensusDecision previousDecision;
        private readonly ILeaseProvider leaseProvider;
        private readonly IConsensus consensus;
        private readonly ILogger logger;
        private IPerformanceCounter consensusPerSecond;

        public Rsm(IReplicatedLog replicatedLog,
                   ILeaseProvider leaseProvider,
                   IConsensus consensus,
                   IPerformanceCountersCategory<WacsPerformanceCounters> perfCounters,
                   ILogger logger)
        {
            this.logger = logger;
            commandsQueue = new BlockingCollection<IAwaitableResponse<IMessage>>(new ConcurrentQueue<IAwaitableResponse<IMessage>>());
            this.replicatedLog = replicatedLog;
            this.leaseProvider = leaseProvider;
            this.consensus = consensus;
            consensusPerSecond = perfCounters.GetCounter(WacsPerformanceCounters.ConsensusAgreementsPerSecond);

            processingThread = new Thread(ProcessCommands);
            processingThread.Start();
        }

        private void ProcessCommands()
        {
            foreach (var awaitableResult in commandsQueue.GetConsumingEnumerable())
            {
                try
                {
                    var command = awaitableResult;
                    do
                    {
                        command = ProcessCommand(command);
                    } while (command != null);
                }
                catch (Exception err)
                {
                    logger.Error(err);
                }
            }
        }

        private IAwaitableResponse<IMessage> ProcessCommand(IAwaitableResponse<IMessage> awaitableResponse)
        {
            var firstUnchosenLogEntry = replicatedLog.GetFirstUnchosenLogEntryIndex();
            var awaitableRequest = (AwaitableRsmRequest) awaitableResponse;

            var decision = consensus.Decide(firstUnchosenLogEntry, awaitableRequest, RoundCouldBeFast());

            consensusPerSecond.Increment();

            if (ConsensusNotReachedDueToShortHistoryPrefix(decision))
            {
                leaseProvider.ResetLease();
            }

            if (decision.Outcome == ConsensusOutcome.DecidedWithProposedValue)
            {
                return null;
            }

            return awaitableResponse;
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

        public IAwaitableResponse<IMessage> EnqueueForExecution(IMessage command)
        {
            var awaitableRsmResponse = new AwaitableRsmRequest(command);
            commandsQueue.Add(awaitableRsmResponse);

            return awaitableRsmResponse;
        }

        void IDisposable.Dispose()
        {
            commandsQueue.CompleteAdding();
            processingThread.Join();
            commandsQueue.Dispose();
            consensus.Dispose();
        }
    }
}