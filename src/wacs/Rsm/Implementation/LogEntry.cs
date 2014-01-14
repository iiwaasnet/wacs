using wacs.Messaging.Messages;
using wacs.Rsm.Interface;

namespace wacs.Rsm.Implementation
{
    public class LogEntry : ILogEntry
    {
        public IMessage Value { get; set; }
        public LogEntryState State { get; set; }
    }
}