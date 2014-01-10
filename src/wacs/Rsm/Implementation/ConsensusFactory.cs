using wacs.Messaging.Messages;
using wacs.Rsm.Interface;

namespace wacs.Rsm.Implementation
{
    public class ConsensusFactory : IConsensusFactory
    {
        public IConsensus CreateInstance(ILogIndex index, IMessage command, bool fast)
        {
            return new Consensus();
        }
    }
}