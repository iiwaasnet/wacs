namespace wacs.Rsm.Interface
{
    public interface ILogIndex
    {
        ILogIndex Increment();
        ulong Index { get; }
    }
}