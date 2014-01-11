namespace wacs.Messaging.Messages.Intercom.Rsm
{
    public class Ballot
    {
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
            return Equals((Ballot) obj);
        }

        public override int GetHashCode()
        {
            return ProposalNumber.GetHashCode();
        }

        public ulong ProposalNumber { get; set; }
    }
}