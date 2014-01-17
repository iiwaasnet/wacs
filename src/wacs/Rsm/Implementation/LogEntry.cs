using wacs.Messaging.Messages;
using wacs.Rsm.Interface;

namespace wacs.Rsm.Implementation
{
    public class LogEntry : ILogEntry
    {
        public LogEntry(IMessage value, ILogIndex index, LogEntryState state)
        {
            Value = value;
            Index = index;
            State = state;
        }

        public IMessage Value { get; private set; }
        public LogEntryState State { get; private set; }
        public ILogIndex Index { get; private set; }
    }
}