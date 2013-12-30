using wacs.Messaging;
using wacs.Resolver.Implementation;

namespace wacs.FLease.Messages
{
    public class LeaseAckRead : TypedMessage<LeaseAckRead.Payload>
    {
        static LeaseAckRead()
        {
            MessageType = "LEASE_ACKREAD";
        }

        public LeaseAckRead(IMessage message)
            : base(message)
        {
        }

        public LeaseAckRead(IProcess sender, Payload payload)
            : base(sender, payload)
        {
        }

        public class Payload : ILeaseMessagePayload
        {
            public Ballot Ballot { get; set; }
            public Ballot KnownWriteBallot { get; set; }
            public Lease Lease { get; set; }
        }
    }
}