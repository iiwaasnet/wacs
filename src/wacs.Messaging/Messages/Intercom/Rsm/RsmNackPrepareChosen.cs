namespace wacs.Messaging.Messages.Intercom.Rsm
{
    public class RsmNackPrepareChosen : TypedMessage<RsmNackPrepareChosen.Payload>
    {
        public RsmNackPrepareChosen(IMessage message)
            : base(message)
        {
        }

        public RsmNackPrepareChosen(Process sender, Payload payload)
            : base(sender, payload, MessageType)
        {
        }

        public static string MessageType
        {
            get { return "RSM_NACK_PREPARE_CHOSEN"; }
        }

        public class Payload : IConsensusDecisionPayload
        {
            public LogIndex LogIndex { get; set; }
            public Ballot Proposal { get; set; }
        }
    }
}