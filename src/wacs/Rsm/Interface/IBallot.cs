using System;
using wacs.Rsm.Implementation;

namespace wacs.Rsm.Interface
{
    public interface IBallot : IComparable
    {
        Ballot NewByIncrementing();

        ulong ProposalNumber { get; }
    }
}