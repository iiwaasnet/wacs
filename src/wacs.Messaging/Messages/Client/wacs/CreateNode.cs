using wacs.Configuration;

namespace wacs.Messaging.Messages.Client.wacs
{
    public class CreateNode : TypedMessage<CreateNode.Payload>
    {
        public CreateNode(IMessage message)
            : base(message)
        {
        }

        public CreateNode(IProcess sender, Payload payload)
            : base(sender, payload, MessageType)
        {
        }

        public static string MessageType
        {
            get { return "WACS_CREATE_NODE"; }
        }

        public class Payload
        {
            
        }
    }
}