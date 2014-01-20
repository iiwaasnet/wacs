namespace wacs.Messaging.Messages.Intercom.Rsm
{
    public class RsmNackAcceptBlocked : TypedMessage<RsmNackAcceptBlocked.Payload>
    {
        public RsmNackAcceptBlocked(IMessage message)
            : base(message)
        {
        }

        public RsmNackAcceptBlocked(Process sender, Payload payload)
            : base(sender, payload, MessageType)
        {
        }

        public static string MessageType
        {
            get { return "RSM_NACK_ACCEPT_BLOCKED"; }
        }

        public class Payload : IConsensusDecisionPayload
        {
            public LogIndex LogIndex { get; set; }
            public Ballot Proposal { get; set; }
            public Ballot MinProposal { get; set; }
        }
    }
}