using wacs.Configuration;

namespace wacs.Messaging.Messages.Intercom.Rsm
{
    public class PrepareMessage : TypedMessage<PrepareMessage.Payload>
    {
        public PrepareMessage(IMessage message)
            : base(message)
        {
        }

        public PrepareMessage(IProcess sender, Payload payload)
            : base(sender, payload, MessageType)
        {
        }

        public static string MessageType
        {
            get { return "RSM_PREPARE"; }
        }

        public class Payload
        {
            public int LeaderProcessId { get; set; }
        }
    }
}