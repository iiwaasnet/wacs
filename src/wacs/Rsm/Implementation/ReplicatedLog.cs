using wacs.Rsm.Interface;

namespace wacs.Rsm.Implementation
{
    public class ReplicatedLog : IReplicatedLog
    {
        public ILogEntry GetLogEntry(ILogIndex iid)
        {
            throw new System.NotImplementedException();
        }

        public void SetLogEntry(ILogIndex iid, ILogEntry value)
        {
            throw new System.NotImplementedException();
        }

        public ILogIndex GetFirstUnchosenLogEntry()
        {
            throw new System.NotImplementedException();
        }
    }
}