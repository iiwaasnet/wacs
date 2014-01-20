namespace wacs.Messaging.Messages.Intercom.NodeResolver
{
    public class ProcessAnnouncementMessage : TypedMessage<ProcessAnnouncementMessage.Payload>
    {
        public ProcessAnnouncementMessage(IMessage message)
            : base(message)
        {
        }

        public ProcessAnnouncementMessage(Process sender, Payload payload)
            : base(sender, payload, MessageType)
        {
        }

        public static string MessageType
        {
            get { return "PROC_ANN"; }
        }

        public class Payload
        {
            public Node Node { get; set; }
            public Process Process { get; set; }
        }
    }
}