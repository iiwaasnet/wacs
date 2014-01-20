namespace wacs.Messaging.Messages.Intercom.Rsm
{
    public class RsmNackAcceptChosen : TypedMessage<RsmNackAcceptChosen.Payload>
    {
        public RsmNackAcceptChosen(IMessage message)
            : base(message)
        {
        }

        public RsmNackAcceptChosen(Process sender, Payload payload)
            : base(sender, payload, MessageType)
        {
        }

        public static string MessageType
        {
            get { return "RSM_NACK_ACCEPT_CHOSEN"; }
        }

        public class Payload : IConsensusDecisionPayload
        {
            public LogIndex LogIndex { get; set; }
            public Ballot Proposal { get; set; }
        }
    }
}