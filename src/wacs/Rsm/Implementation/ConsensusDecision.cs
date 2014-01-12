using wacs.Messaging.Messages;
using wacs.Rsm.Interface;

namespace wacs.Rsm.Implementation
{
    public class ConsensusDecision : IConsensusDecision
    {
        public bool NextRoundCouldBeFast { get; set; }
        public IMessage DecidedValue { get; set; }
        public ConsensusOutcome Outcome { get; set; }
    }
}