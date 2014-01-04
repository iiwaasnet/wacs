using wacs.FLease;

namespace wacs.Rsm.Interface
{
    public interface ILogEntry
    {
        IBallot MinProposal { get; }
        IBallot AcceptedProposal { get; }
        IValue Value { get; }
        LogEntryState State { get; }
    }
}