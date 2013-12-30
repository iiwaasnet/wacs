using wacs.Messaging;
using wacs.Resolver.Implementation;

namespace wacs.FLease.Messages
{
    public class LeaseRead : TypedMessage<LeaseRead.Payload>
    {
        static LeaseRead()
        {
            MessageType = "LEASE_READ";
        }

        public LeaseRead(IMessage message)
            : base(message)
        {
        }

        public LeaseRead(IProcess sender, Payload payload)
            : base(sender, payload)
        {
        }

        public class Payload : ILeaseMessagePayload
        {
            public Ballot Ballot { get; set; }
        }
    }
}