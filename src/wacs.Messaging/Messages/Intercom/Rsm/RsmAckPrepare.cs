using wacs.Configuration;

namespace wacs.Messaging.Messages.Intercom.Rsm
{
    public class RsmAckPrepare : TypedMessage<RsmAckPrepare.Payload>
    {
        public RsmAckPrepare(IMessage message)
            : base(message)
        {
        }

        public RsmAckPrepare(IProcess sender, Payload payload)
            : base(sender, payload, MessageType)
        {
        }

        public static string MessageType
        {
            get { return "RSM_ACK_PREPARE"; }
        }

        public class Payload : IPreparePayload
        {
            public LogIndex LogIndex { get; set; }
            public Ballot Proposal { get; set; }
            public Ballot AcceptedProposal { get; set; }
            public Message AcceptedValue { get; set; }
        }
    }
}