using wacs.Configuration;

namespace wacs.Messaging.Messages.Intercom.Rsm
{
    public class RsmNackPrepareBlocked : TypedMessage<RsmNackPrepareBlocked.Payload>
    {
        public RsmNackPrepareBlocked(IMessage message)
            : base(message)
        {
        }

        public RsmNackPrepareBlocked(IProcess sender, Payload payload)
            : base(sender, payload, MessageType)
        {
        }

        public static string MessageType
        {
            get { return "RSM_NACK_PREPARE_BLOCKED"; }
        }

        public class Payload : IPreparePayload
        {
            public LogIndex LogIndex { get; set; }
            public Ballot Ballot { get; set; }
            public Ballot AcceptedBallot { get; set; }
        }
    }
}