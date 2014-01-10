using wacs.Messaging.Messages;

namespace wacs.Rsm.Interface
{
    public interface IConsensusFactory
    {
        IConsensus CreateInstance(ILogIndex index, IMessage command, bool fast);
    }
}