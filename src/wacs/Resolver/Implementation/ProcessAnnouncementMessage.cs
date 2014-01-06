using wacs.Messaging;

namespace wacs.Resolver.Implementation
{
    public class ProcessAnnouncementMessage : TypedMessage<ProcessAnnouncementMessage.Payload>
    {
        public ProcessAnnouncementMessage(IMessage message)
            : base(message)
        {
        }

        public ProcessAnnouncementMessage(IProcess sender, Payload payload)
            : base(sender, payload, MessageType)
        {
        }

        public static string MessageType
        {
            get { return "PROC_ANN"; }
        }

        public class Payload
        {
            public string BaseAddress { get; set; }
            public int IntercomPort { get; set; }
            public int ServicePort { get; set; }
            public int ProcessId { get; set; }
        }
    }
}