using wacs.Messaging.Messages;

namespace wacs.Rsm.Interface
{
    public delegate void ValueChosenHandler();
    public interface IReplicatedLog
    {
        ILogEntry GetLogEntry(ILogIndex iid);

        void SetLogEntryAccepted(ILogIndex iid, IMessage value);

        void SetLogEntryChosen(ILogIndex iid, IMessage value);

        ILogIndex GetFirstUnchosenLogEntryIndex();

        void TruncateLog(ILogIndex truncateBeforeLogIndex);

        event ValueChosenHandler ValueChosen;
    }
}