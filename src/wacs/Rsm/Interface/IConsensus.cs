using wacs.Messaging.Messages;

namespace wacs.Rsm.Interface
{
    public interface IConsensus
    {
        IConsensusDecision Decide(ILogIndex logIndex, IMessage command, bool fast);
    }
}