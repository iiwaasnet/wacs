using System.Runtime.Remoting.Messaging;
using wacs.FLease;

namespace wacs.Rsm.Implementation
{
    internal class PreparePhaseResult
    {
        internal IMessage AcceptedValue { get; set; }
        internal PreparePhaseOutcome Outcome { get; set; }
        internal IBallot AcceptedBallot { get; set; }
    }

    internal enum PreparePhaseOutcome
    {
        SucceededWithProposedValue,
        SucceededWithOtherValue,
        FailedDueToLowBallot,
        FailedDueToChosenLogEntry
    }
}