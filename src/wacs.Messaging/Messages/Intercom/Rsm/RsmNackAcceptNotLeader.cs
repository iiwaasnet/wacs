using wacs.Configuration;

namespace wacs.Messaging.Messages.Intercom.Rsm
{
    public class RsmNackAcceptNotLeader : TypedMessage<RsmNackAcceptNotLeader.Payload>
    {
        public RsmNackAcceptNotLeader(IMessage message)
            : base(message)
        {
        }

        public RsmNackAcceptNotLeader(IProcess sender, Payload payload)
            : base(sender, payload, MessageType)
        {
        }

        public static string MessageType
        {
            get { return "RSM_NACK_ACCEPT_NOTLEADER"; }
        }

        public class Payload : IConsensusDecisionPayload
        {
            public LogIndex LogIndex { get; set; }
            public Ballot Proposal { get; set; }
        }
    }
}