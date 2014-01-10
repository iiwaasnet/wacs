using wacs.Configuration;

namespace wacs.Messaging.Messages.Intercom.NodeResolver
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

        public class Node
        {
            public string BaseAddress { get; set; }
            public int IntercomPort { get; set; }
            public int ServicePort { get; set; }
        }

        public class Payload
        {
            public Node Node { get; set; }
            public Process Process { get; set; }
        }

        public class Process
        {
            public int ProcessId { get; set; }
        }
    }
}