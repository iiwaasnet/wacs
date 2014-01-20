namespace wacs.Messaging.Messages.Intercom.Rsm
{
    public class RsmAccept : TypedMessage<RsmAccept.Payload>
    {
        public RsmAccept(IMessage message)
            : base(message)
        {
        }

        public RsmAccept(Process sender, Payload payload)
            : base(sender, payload, MessageType)
        {
        }

        public static string MessageType
        {
            get { return "RSM_ACCEPT"; }
        }

        public class Payload : IConsensusDecisionPayload
        {
            public Process Leader { get; set; }
            public Ballot Proposal { get; set; }
            public LogIndex LogIndex { get; set; }
            public Message Value { get; set; }
        }
    }
}