using wacs.Messaging;
using wacs.Resolver.Implementation;

namespace wacs.FLease.Messages
{
    public class LeaseNackRead : TypedMessage<LeaseNackRead.Payload>
    {
        static LeaseNackRead()
        {
            MessageType = "LEASE_NACKREAD";
        }

        public LeaseNackRead(IMessage message) 
            : base(message)
        {
        }

        public LeaseNackRead(IProcess sender, Payload payload)
            : base(sender, payload)
        {
        }

        
        public class Payload : ILeaseMessagePayload
        {
            public Ballot Ballot { get; set; }
        }
    }
}