using System;

namespace wacs.Messaging.Messages.Intercom.Rsm
{
    public class Ballot : IComparable
    {
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
            return Equals((Ballot) obj);
        }

        public override int GetHashCode()
        {
            return ProposalNumber.GetHashCode();
        }

        public ulong ProposalNumber { get; set; }
    }
}