﻿namespace wacs.Messaging.Messages.Intercom.Rsm
{
    public class RsmNackPrepareBlocked : TypedMessage<RsmNackPrepareBlocked.Payload>
    {
        public RsmNackPrepareBlocked(IMessage message)
            : base(message)
        {
        }

        public RsmNackPrepareBlocked(Process sender, Payload payload)
            : base(sender, payload, MessageType)
        {
        }

        public static string MessageType
        {
            get { return "RSM_NACK_PREPARE_BLOCKED"; }
        }

        public class Payload : IConsensusDecisionPayload
        {
            public LogIndex LogIndex { get; set; }
            public Ballot Proposal { get; set; }
            public Ballot MinProposal { get; set; }
        }
    }
}