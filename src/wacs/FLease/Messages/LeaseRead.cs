using wacs.Messaging;
using wacs.Resolver.Implementation;

namespace wacs.FLease.Messages
{
    public class LeaseRead : TypedMessage<LeaseRead.Payload>
    {
        public LeaseRead(IMessage message)
            : base(message)
        {
        }

        public LeaseRead(IProcess sender, Payload payload)
            : base(sender, payload, MessageType)
        {
        }

        public static string MessageType
        {
            get { return "LEASE_READ"; }
        }

        public class Payload : ILeaseMessagePayload
        {
            public Ballot Ballot { get; set; }
        }
    }
}