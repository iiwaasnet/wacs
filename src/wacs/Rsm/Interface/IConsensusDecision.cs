using wacs.Messaging.Messages;

namespace wacs.Rsm.Interface
{
    public interface IConsensusDecision
    {
        bool NextRoundCouldBeFast { get; }
        IMessage DecidedValue { get; }
        ConsensusOutcome Outcome { get; }
    }

    public enum ConsensusOutcome
    {
        DecidedWithProposedValue,
        DecidedWithOtherValue,
        RejectedDueToChosenLogEntry,
        FailedDueToNotBeingLeader,
        FailedDueToLowBallot
    }
}