namespace wacs.Rsm.Interface
{
    public interface ISnapshot
    {
        ILogIndex LastAppliedCommandIndex { get; }
    }
}