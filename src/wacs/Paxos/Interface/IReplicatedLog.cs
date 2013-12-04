namespace wacs.Paxos.Interface
{
    public interface IReplicatedLog
    {
        ILogEntry GetLogEntry(ILogIndex iid);
    }
}