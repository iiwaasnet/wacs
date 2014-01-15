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

        public static bool operator <=(Ballot x, Ballot y)
        {
            var res = x.CompareTo(y);

            return res < 0 || res == 0;
        }

        public static bool operator >=(Ballot x, Ballot y)
        {
            var res = x.CompareTo(y);

            return res > 0 || res == 0;
        }

        public static bool operator <(Ballot x, Ballot y)
        {
            return x.CompareTo(y) < 0;
        }

        public static bool operator >(Ballot x, Ballot y)
        {
            return x.CompareTo(y) > 0;
        }

        public int CompareTo(object obj)
        {
            var ballot = obj as Ballot;

            return ProposalNumber.CompareTo(ballot.ProposalNumber);
        }

        protected bool Equals(Ballot other)
        {
            return ProposalNumber == other.ProposalNumber;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }
            if (ReferenceEquals(this, obj))
            {
                return true;
            }
            if (obj.GetType() != this.GetType())
            {
                return false;
            }
            return Equals((Ballot)obj);
        }

        public override int GetHashCode()
        {
            return ProposalNumber.GetHashCode();
        }

        public Ballot Incrementing()
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