using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Remoting.Messaging;
using System.Threading;
using System.Windows.Forms;
using wacs.Diagnostics;
using wacs.Messaging.Hubs.Intercom;
using wacs.Rsm.Interface;

namespace wacs.Rsm.Implementation
{
    public class ReplicatedState : IReplicatedState
    {
        private readonly IReplicatedLog replicatedLog;
        private readonly ILogger logger;
        private readonly AutoResetEvent stateChangeGateway;
        private readonly CancellationTokenSource cancellationSource;
        private readonly Thread workerThread;
        private readonly TimeSpan nextCommandWaitTimeout;
        private ILogIndex lastAppliedCommandIndex;
        private readonly IIntercomMessageHub intercomMessageHub;

        public ReplicatedState(IReplicatedLog replicatedLog, IIntercomMessageHub intercomMessageHub, ILogger logger)
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
                ProcessCommand(logEntry.Value);
                lastAppliedCommandIndex = logEntry.Index;
            }

            stateChangeGateway.Reset();
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