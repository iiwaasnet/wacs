namespace wacs.Messaging.Messages.Intercom.Lease
{
    public class LeaseAckWrite : TypedMessage<LeaseAckWrite.Payload>
    {
        public LeaseAckWrite(IMessage message)
            : base(message)
        {
        }

        public LeaseAckWrite(Process sender, Payload payload)
            : base(sender, payload, MessageType)
        {
        }

        public static string MessageType
        {
            get { return "LEASE_ACKWRITE"; }
        }

        public class Payload : ILeaseMessagePayload
        {
            public Ballot Ballot { get; set; }
        }
    }
}