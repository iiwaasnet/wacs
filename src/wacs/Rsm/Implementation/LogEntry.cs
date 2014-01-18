using wacs.Rsm.Interface;

namespace wacs.Rsm.Implementation
{
    public class LogEntry : ILogEntry
    {
        public LogEntry(ISyncCommand command, ILogIndex index, LogEntryState state)
        {
            Command = command;
            Index = index;
            State = state;
        }

        public ISyncCommand Command { get; private set; }
        public LogEntryState State { get; private set; }
        public ILogIndex Index { get; private set; }
    }
}