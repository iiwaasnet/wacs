namespace wacs.Messaging.Messages.Intercom.Lease
{
    public class LeaseAckRead : TypedMessage<LeaseAckRead.Payload>
    {
        public LeaseAckRead(IMessage message)
            : base(message)
        {
        }

        public LeaseAckRead(Process sender, Payload payload)
            : base(sender, payload, MessageType)
        {
        }

        public static string MessageType
        {
            get { return "LEASE_ACKREAD"; }
        }

        public class Payload : ILeaseMessagePayload
        {
            public Ballot Ballot { get; set; }
            public Ballot KnownWriteBallot { get; set; }
            public Lease Lease { get; set; }
        }
    }
}