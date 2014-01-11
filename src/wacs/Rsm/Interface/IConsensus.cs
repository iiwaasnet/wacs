using wacs.Messaging.Messages;

namespace wacs.Rsm.Interface
{
    public interface IConsensus
    {
        IDecision Decide(ILogIndex index, IMessage command, bool fast);
    }
}