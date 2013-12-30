using wacs.Messaging;

namespace wacs.Resolver.Implementation
{
    public class ProcessAnnouncementMessage : TypedMessage<ProcessAnnouncementMessage.Payload>
    {
        static ProcessAnnouncementMessage()
        {
            MessageType = "PROC_ANN";
        }

        public ProcessAnnouncementMessage(IMessage message)
            : base(message)
        {
        }

        public ProcessAnnouncementMessage(IProcess sender, Payload payload)
            : base(sender, payload)
        {
        }

        public class Payload
        {
            public string Endpoint { get; set; }
            public int ProcessId { get; set; }
        }
    }
}