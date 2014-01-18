namespace wacs.Rsm.Interface
{
    public interface ILogEntry
    {
        ISyncCommand Command { get; }
        LogEntryState State { get; }
        ILogIndex Index { get; }
    }
}