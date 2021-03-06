﻿namespace wacs.Messaging.Messages.Intercom.Rsm
{
    public class RsmNackPrepareNotLeader : TypedMessage<RsmNackPrepareNotLeader.Payload>
    {
        public RsmNackPrepareNotLeader(IMessage message)
            : base(message)
        {
        }

        public RsmNackPrepareNotLeader(Process sender, Payload payload)
            : base(sender, payload, MessageType)
        {
        }

        public static string MessageType
        {
            get { return "RSM_NACK_PREPARE_NOTLEADER"; }
        }

        public class Payload : IConsensusDecisionPayload
        {
            public LogIndex LogIndex { get; set; }
            public Ballot Proposal { get; set; }
        }
    }
}