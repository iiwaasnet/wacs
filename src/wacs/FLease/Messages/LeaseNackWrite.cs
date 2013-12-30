using wacs.Messaging;
using wacs.Resolver.Implementation;

namespace wacs.FLease.Messages
{
    public class LeaseNackWrite : TypedMessage<LeaseNackWrite.Payload>
    {
        static LeaseNackWrite()
        {
            MessageType = "LEASE_NACKWRITE";
        }

        public LeaseNackWrite(IMessage message)
            : base(message)
        {
        }

        public LeaseNackWrite(IProcess sender, Payload payload)
            : base(sender, payload)
        {
        }

        public class Payload : ILeaseMessagePayload
        {
            public Ballot Ballot { get; set; }
        }
    }
}