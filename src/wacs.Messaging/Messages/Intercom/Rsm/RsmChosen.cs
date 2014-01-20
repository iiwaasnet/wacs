namespace wacs.Messaging.Messages.Intercom.Rsm
{
    public class RsmChosen : TypedMessage<RsmChosen.Payload>
    {
        public RsmChosen(IMessage message)
            : base(message)
        {
        }

        public RsmChosen(Process sender, Payload payload)
            : base(sender, payload, MessageType)
        {
        }

        public static string MessageType
        {
            get { return "RSM_CHOSEN"; }
        }

        public class Payload
        {
            public Process Leader { get; set; }
            public LogIndex LogIndex { get; set; }
            public Message Value { get; set; }
        }
    }
}