using wacs.Configuration;

namespace wacs.Messaging.Messages.Client.Error
{
    public class ErrorMessage : TypedMessage<ErrorMessage.Payload>
    {
        public ErrorMessage(IMessage message)
            : base(message)
        {
        }

        public ErrorMessage(IProcess sender, Payload payload)
            : base(sender, payload, MessageType)
        {
        }

        public static string MessageType
        {
            get { return "ERROR"; }
        }

        public class Payload
        {
            public string ErrorCode { get; set; }
            public string Error { get; set; }
            public string NodeAddress { get; set; }
            public int ProcessId { get; set; }
        }
    }
}