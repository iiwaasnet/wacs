using wacs.Messaging;
using wacs.Resolver.Implementation;

namespace wacs.FLease.Messages
{
    public class LeaseWrite : TypedMessage<LeaseWrite.Payload>
    {
        static LeaseWrite()
        {
            MessageType = "LEASE_WRITE";
        }

        public LeaseWrite(IMessage message)
            : base(message)
        {
        }

        public LeaseWrite(IProcess sender, Payload payload)
            : base(sender, payload)
        {
        }

        public class Payload : ILeaseMessagePayload
        {
            public Ballot Ballot { get; set; }
            public Lease Lease { get; set; }
        }
    }
}