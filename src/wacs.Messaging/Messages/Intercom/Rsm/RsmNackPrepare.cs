using wacs.Configuration;

namespace wacs.Messaging.Messages.Intercom.Rsm
{
    public class RsmNackPrepare : TypedMessage<RsmNackPrepare.Payload>
    {
        public RsmNackPrepare(IMessage message)
            : base(message)
        {
        }

        public RsmNackPrepare(IProcess sender, Payload payload)
            : base(sender, payload, MessageType)
        {
        }

        public static string MessageType
        {
            get { return "RSM_NACK_PREPARE"; }
        }

        public class Payload
        {
            public LogIndex LogIndex { get; set; }
            public Ballot PrepareBallot { get; set; }
            public Ballot AcceptedBallot { get; set; }
        }
    }
}