using wacs.Configuration;

namespace wacs.Messaging.Messages.Client.wacs
{
    public class CreateNodeRequest : TypedMessage<CreateNodeRequest.Payload>
    {
        public CreateNodeRequest(IMessage message)
            : base(message)
        {
        }

        public CreateNodeRequest(IProcess sender, Payload payload)
            : base(sender, payload, MessageType)
        {
        }

        public static string MessageType
        {
            get { return "WACS_REQ_CREATE_NODE"; }
        }

        public class Payload
        {
            public string NodeName { get; set; }
        }
    }
}