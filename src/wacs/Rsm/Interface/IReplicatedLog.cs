namespace wacs.Rsm.Interface
{
    public interface IReplicatedLog
    {
        ILogEntry GetLogEntry(ILogIndex iid);
        ILogIndex GetFirstUnchosenLogEntry();
    }
}