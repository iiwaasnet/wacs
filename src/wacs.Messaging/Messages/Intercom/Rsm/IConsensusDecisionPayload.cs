namespace wacs.Messaging.Messages.Intercom.Rsm
{
    public interface IConsensusDecisionPayload
    {
        LogIndex LogIndex { get; }
        Ballot Proposal { get; } 
    }
}