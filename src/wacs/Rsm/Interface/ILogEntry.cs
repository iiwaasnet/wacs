namespace wacs.Rsm.Interface
{
    public interface ILogEntry
    {
        IValue Value { get; }
        LogEntryState State { get; }
    }
}