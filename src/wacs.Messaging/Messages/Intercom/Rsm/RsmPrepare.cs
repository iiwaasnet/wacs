using wacs.Configuration;

namespace wacs.Messaging.Messages.Intercom.Rsm
{
    public class RsmPrepare : TypedMessage<RsmPrepare.Payload>
    {
        public RsmPrepare(IMessage message)
            : base(message)
        {
        }

        public RsmPrepare(IProcess sender, Payload payload)
            : base(sender, payload, MessageType)
        {
        }

        public static string MessageType
        {
            get { return "RSM_PREPARE"; }
        }

        public class Payload : IConsensusDecisionPayload
        {
            public Process Leader { get; set; }
            public Ballot Proposal { get; set; }
            public LogIndex LogIndex { get; set; }
        }
    }
}