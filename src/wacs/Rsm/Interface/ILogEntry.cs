using wacs.Messaging.Messages;

namespace wacs.Rsm.Interface
{
    public interface ILogEntry
    {
        IMessage Value { get; }
        LogEntryState State { get; }
    }
}