using System;
using System.Collections.Concurrent;
using System.Threading;
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

        public Rsm(IReplicatedLog replicatedLog, IConsensusFactory consensusFactory)
        {
            commandsQueue = new BlockingCollection<IAwaitableResult<IMessage>>(new ConcurrentQueue<IAwaitableResult<IMessage>>());
            this.replicatedLog = replicatedLog;
            cancellationSource = new CancellationTokenSource();
            this.consensusFactory = consensusFactory;

            processingThread = new Thread(()=> ProcessCommands(cancellationSource.Token));
            processingThread.Start();
        }

        private void ProcessCommands(CancellationToken token)
        {
            foreach (var awaitableResult in commandsQueue.GetConsumingEnumerable(token))
            {
                ProcessCommand(awaitableResult);
            }
        }

        private void ProcessCommand(IAwaitableResult<IMessage> awaitableResult)
        {
            var firstUnchosenLogEntry = replicatedLog.GetFirstUnchosenLogEntry();
            var awaitable = (AwaitableRsmResponse) awaitableResult;
            var consensus = consensusFactory.CreateInstance();
            previousDecision = consensus.Decide(firstUnchosenLogEntry, awaitable.Command, RoundCouldBeFast());
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