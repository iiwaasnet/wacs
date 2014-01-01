using wacs.Messaging;
using wacs.Resolver.Implementation;

namespace wacs.FLease.Messages
{
    public class LeaseAckWrite : TypedMessage<LeaseAckWrite.Payload>
    {
        public LeaseAckWrite(IMessage message)
            : base(message)
        {
        }

        public LeaseAckWrite(IProcess sender, Payload payload)
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