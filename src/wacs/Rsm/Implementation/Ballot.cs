using wacs.Configuration;
using wacs.Rsm.Interface;

namespace wacs.Rsm.Implementation
{
    public class Ballot : IBallot
    {
        private const int ProcessWeight = 1000;
        private readonly int processId;
        private readonly ulong proposalId;
        public Ballot(ulong proposalNumber, IProcess process)
            :this(proposalNumber, process.Id)
        {
        }

        private Ballot(ulong proposalNumber, int processId)
        {
            this.processId = processId;
            proposalId = proposalNumber;
            ProposalNumber = CreateProposalNumber();
        }

        public Ballot NewByIncrementing()
        {
            return new Ballot(proposalId +1, processId);
        }

        private ulong CreateProposalNumber()
        {
            return proposalId * ProcessWeight + (ulong)processId;
        }

        public ulong ProposalNumber { get; private set; }
    }
}