using wacs.Messaging.Messages;
using wacs.Rsm.Interface;
using IMessage = wacs.Messaging.Messages.IMessage;

namespace wacs.Rsm.Implementation
{
    internal class PreparePhaseResult
    {
        internal IMessage AcceptedValue { get; set; }
        internal PreparePhaseOutcome Outcome { get; set; }
        internal IBallot MinProposal { get; set; }
    }

    internal enum PreparePhaseOutcome
    {
        SucceededWithProposedValue,
        SucceededWithOtherValue,
        FailedDueToLowBallot,
        FailedDueToChosenLogEntry,
        FailedDueToNotBeingLeader
    }
}