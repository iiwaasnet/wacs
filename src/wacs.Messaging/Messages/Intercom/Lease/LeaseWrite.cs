using wacs.Configuration;

namespace wacs.Messaging.Messages.Intercom.Lease
{
    public class LeaseWrite : TypedMessage<LeaseWrite.Payload>
    {
        public LeaseWrite(IMessage message)
            : base(message)
        {
        }

        public LeaseWrite(IProcess sender, Payload payload)
            : base(sender, payload, MessageType)
        {
        }

        public static string MessageType
        {
            get { return "LEASE_WRITE"; }
        }

        public class Payload : ILeaseMessagePayload
        {
            public Ballot Ballot { get; set; }
            public Lease Lease { get; set; }
        }
    }
}