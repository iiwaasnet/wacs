using System;
using System.Collections.Generic;
using System.ComponentModel;
using wacs.Messaging.Messages;
using wacs.Rsm.Interface;

namespace wacs.Rsm.Implementation
{
    public class ReplicatedLog : IReplicatedLog
    {
        private readonly IDictionary<ILogIndex, ILogEntry> log;
        private readonly object locker = new object();
        private ILogIndex firstUnchosenIndex;
        private ILogIndex lowestChosenIndex;
        private ILogIndex firstLogIndex;
        private readonly EventHandlerList eventHandlers;
        private readonly object ValueChosenEvent = new object();

        public ReplicatedLog()
        {
            log = new Dictionary<ILogIndex, ILogEntry>();
            firstUnchosenIndex = firstLogIndex = lowestChosenIndex = new LogIndex(0);
            eventHandlers = new EventHandlerList();
        }

        public ILogEntry GetLogEntry(ILogIndex iid)
        {
            lock (locker)
            {
                return log[iid];
            }
        }

        public void SetLogEntryAccepted(ILogIndex iid, IMessage value)
        {
            lock (locker)
            {
                AssertLogEntryNotChosen(iid);

                log[iid] = new LogEntry(value, iid, LogEntryState.Accepted);
            }
        }

        public void SetLogEntryChosen(ILogIndex iid, IMessage value)
        {
            lock (locker)
            {
                AssertLogEntryNotChosen(iid);

                log[iid] = new LogEntry(value, iid, LogEntryState.Chosen);

                if (iid.Equals(firstUnchosenIndex))
                {
                    firstUnchosenIndex = FindNextUnchosenLogEntryIndex(firstUnchosenIndex);
                }
                if (lowestChosenIndex.Increment().Equals(iid))
                {
                    lowestChosenIndex = iid;
                }

                OnValueChosen();
            }
        }

        private ILogIndex FindNextUnchosenLogEntryIndex(ILogIndex currentLogIndex)
        {
            ILogIndex nextLogIndex;
            ILogEntry logEntry;

            do
            {
                nextLogIndex = currentLogIndex.Increment();
            } while (!log.TryGetValue(nextLogIndex, out logEntry) || logEntry.State != LogEntryState.Chosen);

            return nextLogIndex;
        }

        private void AssertLogEntryNotChosen(ILogIndex iid)
        {
            ILogEntry currentValue;

            if ((log.TryGetValue(iid, out currentValue) && currentValue.State == LogEntryState.Chosen)
                || (LogIndex) iid <= (LogIndex) lowestChosenIndex)
            {
                throw new Exception(string.Format("LogEntry with index {0} is already chosen!", iid.Index));
            }
        }

        public ILogIndex GetFirstUnchosenLogEntryIndex()
        {
            lock (locker)
            {
                return firstUnchosenIndex;
            }
        }

        public void TruncateLog(ILogIndex truncateBeforeLogIndex)
        {
            lock (locker)
            {
                CheckTruncationRange(truncateBeforeLogIndex);

                firstLogIndex = lowestChosenIndex = truncateBeforeLogIndex;

                TruncateLogBackFrom(truncateBeforeLogIndex);
            }
        }

        private void CheckTruncationRange(ILogIndex truncateBeforeLogIndex)
        {
            if (!log.ContainsKey(truncateBeforeLogIndex))
            {
                throw new Exception(string.Format("Log index {0} is out of range!", truncateBeforeLogIndex.Index));
            }

            ILogEntry logEntry;
            if (log.TryGetValue(truncateBeforeLogIndex, out logEntry) && logEntry.State != LogEntryState.Chosen)
            {
                throw new Exception(string.Format("Unable to truncate log back from index {0} because it's not chosen!", truncateBeforeLogIndex.Index));
            }
        }

        private void TruncateLogBackFrom(ILogIndex truncateBeforeLogIndex)
        {
            for (var logIndex = truncateBeforeLogIndex.Dicrement(); log.ContainsKey(logIndex); logIndex = logIndex.Dicrement())
            {
                log.Remove(logIndex);
            }
        }

        private void OnValueChosen()
        {
            var handler = eventHandlers[ValueChosenEvent] as ValueChosenHandler;

            if (handler != null)
            {
                handler();
            }
        }

        public event ValueChosenHandler ValueChosen
        {
            add { eventHandlers.AddHandler(ValueChosenEvent, value); }
            remove { eventHandlers.RemoveHandler(ValueChosenEvent, value); }
        }
    }
}