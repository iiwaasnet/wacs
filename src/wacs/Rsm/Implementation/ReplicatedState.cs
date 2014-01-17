using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Remoting.Messaging;
using System.Threading;
using System.Windows.Forms;
using wacs.Diagnostics;
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

        public ReplicatedState(IReplicatedLog replicatedLog, ILogger logger)
        {
            this.logger = logger;
            this.replicatedLog = replicatedLog;
            stateChangeGateway = new AutoResetEvent(false);
            replicatedLog.ValueChosen += OnValueChosen;
            cancellationSource = new CancellationTokenSource();
            nextCommandWaitTimeout = TimeSpan.FromSeconds(3);
            lastAppliedCommandIndex = new LogIndex(0);
            workerThread = new Thread(() => ProcessChosenCommands(cancellationSource.Token));
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