using wacs.Rsm.Implementation;

namespace wacs.Rsm.Interface
{
    public interface IBallot
    {
        Ballot NewByIncrementing();

        ulong ProposalNumber { get; }
    }
}