using wacs.Configuration;

namespace wacs.Messaging.Messages.Client.wacs
{
    public class CreateNodeResponse : TypedMessage<CreateNodeResponse.Payload>
    {
        public CreateNodeResponse(IMessage message)
            : base(message)
        {
        }

        public CreateNodeResponse(IProcess sender, Payload payload)
            : base(sender, payload, MessageType)
        {
        }

        public static string MessageType
        {
            get { return "WACS_RESP_CREATE_NODE"; }
        }

        public class Payload
        {
            public int NodeIndex { get; set; }
        }
    }
}