namespace wacs.Messaging.Messages.Intercom.Rsm
{
    public interface IPreparePayload
    {
        LogIndex LogIndex { get; }
        Ballot Proposal { get; } 
    }
}