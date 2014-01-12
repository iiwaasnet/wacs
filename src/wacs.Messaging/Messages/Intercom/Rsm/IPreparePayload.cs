namespace wacs.Messaging.Messages.Intercom.Rsm
{
    public interface IPreparePayload
    {
        LogIndex LogIndex { get; }
        Ballot PrepareBallot { get; } 
    }
}