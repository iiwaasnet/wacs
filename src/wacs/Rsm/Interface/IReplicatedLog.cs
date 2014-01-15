namespace wacs.Rsm.Interface
{
    public interface IReplicatedLog
    {
        ILogEntry GetLogEntry(ILogIndex iid);

        void SetLogEntry(ILogIndex iid, ILogEntry value);

        ILogIndex GetFirstUnchosenLogEntryIndex();

        void TruncateLog(ILogIndex truncateBeforeLogIndex);
    }
}