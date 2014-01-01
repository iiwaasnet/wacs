using wacs.Messaging;
using wacs.Resolver.Implementation;

namespace wacs.FLease.Messages
{
    public class LeaseNackRead : TypedMessage<LeaseNackRead.Payload>
    {
        public LeaseNackRead(IMessage message) 
            : base(message)
        {
        }

        public LeaseNackRead(IProcess sender, Payload payload)
            : base(sender, payload, MessageType)
        {
        }

        public static string MessageType
        {
            get { return "LEASE_NACKREAD"; }
        }
        
        public class Payload : ILeaseMessagePayload
        {
            public Ballot Ballot { get; set; }
        }
    }
}