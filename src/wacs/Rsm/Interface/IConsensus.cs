namespace wacs.Rsm.Interface
{
    public interface IConsensus
    {
        IConsensusDecision Decide(ILogIndex logIndex, ISyncCommand command, bool fast);
    }
}