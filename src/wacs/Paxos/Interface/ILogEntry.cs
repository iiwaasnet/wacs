using wacs.FLease;

namespace wacs.Paxos.Interface
{
    public interface ILogEntry
    {
        IBallot MinProposal { get; }
        IBallot AcceptedProposal { get; }
        IValue Value { get; }
        LogEntryState State { get; }
    }
}