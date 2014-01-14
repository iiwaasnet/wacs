using wacs.Rsm.Interface;

namespace wacs.Rsm.Implementation
{
    internal class AcceptPhaseResult
    {
        internal IBallot MinProposal { get; set; }
        internal AcceptPhaseOutcome Outcome { get; set; }
    }

    internal enum AcceptPhaseOutcome
    {
        Succeeded,
        FailedDueToLowBallot
    }
}