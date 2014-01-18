using System;
using System.Collections.Generic;
using System.Threading;
using wacs.Diagnostics;
using wacs.Messaging.Hubs.Intercom;
using wacs.Rsm.Interface;

namespace wacs.Rsm.Implementation
{
    public class ReplicationDemultiplexor : IReplicationDemultiplexor
    {
        private readonly IReplicatedLog replicatedLog;
        private readonly ILogger logger;
        private readonly AutoResetEvent stateChangeGateway;
        private readonly CancellationTokenSource cancellationSource;
        private readonly Thread workerThread;
        private readonly TimeSpan nextCommandWaitTimeout;
        private ILogIndex lastAppliedCommandIndex;
        private readonly IIntercomMessageHub intercomMessageHub;
        private readonly IEnumerable<IReplicatedStateMachine> stateMachines;

        public ReplicationDemultiplexor(IReplicatedLog replicatedLog,
                                        IIntercomMessageHub intercomMessageHub,
                                        IEnumerable<IReplicatedStateMachine> stateMachines,
                                        ILogger logger)
        {
            this.logger = logger;
            this.replicatedLog = replicatedLog;
            stateChangeGateway = new AutoResetEvent(false);
            replicatedLog.ValueChosen += OnValueChosen;
            cancellationSource = new CancellationTokenSource();
            nextCommandWaitTimeout = TimeSpan.FromSeconds(3);
            lastAppliedCommandIndex = new LogIndex(0);
            workerThread = new Thread(() => ProcessChosenCommands(cancellationSource.Token));
            this.intercomMessageHub = intercomMessageHub;
            this.stateMachines = stateMachines;

            InitStateFromSnapshot();
        }

        private void InitStateFromSnapshot()
        {
            var snapshot = RequestSnapshot();
            ApplySnapshot(snapshot);

            workerThread.Start();
        }

        private void ApplySnapshot(ISnapshot snapshot)
        {
            lastAppliedCommandIndex = snapshot.LastAppliedCommandIndex;
            throw new NotImplementedException();
        }

        private ISnapshot RequestSnapshot()
        {
            throw new NotImplementedException();
        }

        private void ProcessChosenCommands(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                if (stateChangeGateway.WaitOne(nextCommandWaitTimeout))
                {
                    ProcessNextChosenCommands();
                }
            }
        }

        private void ProcessNextChosenCommands()
        {
            foreach (var logEntry in GetNextChosenLogEntries(lastAppliedCommandIndex.Increment()))
            {
                ProcessCommand(logEntry.Command);
                lastAppliedCommandIndex = logEntry.Index;
            }

            stateChangeGateway.Reset();
        }

        private void ProcessCommand(ISyncCommand command)
        {
            foreach (var stateMachine in stateMachines)
            {
                try
                {
                    stateMachine.ProcessCommand(command);
                }
                catch (Exception err)
                {
                    logger.Error(err);
                }
            }
        }

        private IEnumerable<ILogEntry> GetNextChosenLogEntries(ILogIndex startLogIndex)
        {
            while (true)
            {
                var logEntry = replicatedLog.GetLogEntry(startLogIndex);

                if (logEntry != null && logEntry.State == LogEntryState.Chosen)
                {
                    yield return logEntry;
                }
                else
                {
                    yield break;
                }
            }
        }

        private void OnValueChosen()
        {
            stateChangeGateway.Set();
        }

        public void Dispose()
        {
            cancellationSource.Cancel(false);
            workerThread.Join();
            cancellationSource.Dispose();
        }
    }
}