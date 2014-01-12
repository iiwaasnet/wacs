using wacs.Messaging.Messages;

namespace wacs.Rsm.Interface
{
    public interface IConsensus
    {
        IConsensusDecision Decide(ILogIndex index, IMessage command, bool fast);
    }
}