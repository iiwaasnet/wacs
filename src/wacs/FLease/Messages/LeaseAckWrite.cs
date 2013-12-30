using wacs.Messaging;
using wacs.Resolver.Implementation;

namespace wacs.FLease.Messages
{
    public class LeaseAckWrite : TypedMessage<LeaseAckWrite.Payload>
    {
        static LeaseAckWrite()
        {
            MessageType = "LEASEACKWRITE";
        }

        public LeaseAckWrite(IMessage message)
            : base(message)
        {
        }

        public LeaseAckWrite(IProcess sender, Payload payload)
            : base(sender, payload)
        {
        }

        public class Payload : ILeaseMessagePayload
        {
            public Ballot Ballot { get; set; }
        }
    }
}