using wacs.Messaging;

namespace wacs.Resolver.Implementation
{
    public class ProcessAnnouncementMessage : TypedMessage<ProcessAnnouncementMessage.Payload>
    {
        public const string MessageType = "PROC_ANN";

        public ProcessAnnouncementMessage(IMessage message)
            : base(message)
        {
        }

        public ProcessAnnouncementMessage(IProcess sender, Payload payload)
            : base(sender, payload, MessageType)
        {
        }

        public class Payload
        {
            public string Endpoint { get; set; }
            public int ProcessId { get; set; }
        }
    }
}