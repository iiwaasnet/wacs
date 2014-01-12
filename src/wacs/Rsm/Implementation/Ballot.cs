using wacs.Configuration;
using wacs.Rsm.Interface;

namespace wacs.Rsm.Implementation
{
    public class Ballot : IBallot
    {
        private const int ProcessWeight = 1000;
        private readonly int processId;
        private readonly ulong proposalId;

        public Ballot(ulong proposalId, IProcess process)
            : this(proposalId, process.Id)
        {
        }

        public Ballot(ulong proposalNumber)
            : this(proposalNumber / ProcessWeight, (int) (proposalNumber % ProcessWeight))
        {
        }

        private Ballot(ulong proposalId, int processId)
        {
            this.processId = processId;
            this.proposalId = proposalId;
            ProposalNumber = CreateProposalNumber();
        }

        public Ballot NewByIncrementing()
        {
            return new Ballot(proposalId + 1, processId);
        }

        private ulong CreateProposalNumber()
        {
            return proposalId * ProcessWeight + (ulong) processId;
        }

        public ulong ProposalNumber { get; private set; }
    }
}