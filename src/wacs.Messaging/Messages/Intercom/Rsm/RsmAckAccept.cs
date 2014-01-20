namespace wacs.Messaging.Messages.Intercom.Rsm
{
    public class RsmAckAccept : TypedMessage<RsmAckAccept.Payload>
    {
        public RsmAckAccept(IMessage message)
            : base(message)
        {
        }

        public RsmAckAccept(Process sender, Payload payload)
            : base(sender, payload, MessageType)
        {
        }

        public static string MessageType
        {
            get { return "RSM_ACK_ACCEPT"; }
        }

        public class Payload : IConsensusDecisionPayload
        {
            public LogIndex LogIndex { get; set; }
            public Ballot Proposal { get; set; }
        }
    }
}