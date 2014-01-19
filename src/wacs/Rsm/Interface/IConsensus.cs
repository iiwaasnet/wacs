using System;

namespace wacs.Rsm.Interface
{
    public interface IConsensus : IDisposable
    {
        IConsensusDecision Decide(ILogIndex logIndex, ISyncCommand command, bool fast);
    }
}