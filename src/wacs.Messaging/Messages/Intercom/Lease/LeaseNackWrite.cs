using wacs.Configuration;

namespace wacs.Messaging.Messages.Intercom.Lease
{
    public class LeaseNackWrite : TypedMessage<LeaseNackWrite.Payload>
    {
        public LeaseNackWrite(IMessage message)
            : base(message)
        {
        }

        public LeaseNackWrite(IProcess sender, Payload payload)
            : base(sender, payload, MessageType)
        {
        }

        public static string MessageType
        {
            get { return "LEASE_NACKWRITE"; }
        }

        public class Payload : ILeaseMessagePayload
        {
            public Ballot Ballot { get; set; }
        }
    }
}