using wacs.Rsm.Interface;

namespace wacs.Rsm.Implementation
{
    public class LogIndex : ILogIndex
    {
        public LogIndex(ulong index)
        {
            Index = index;
        }


        public static bool operator <=(LogIndex x, LogIndex y)
        {
            var res = x.CompareTo(y);

            return res < 0 || res == 0;
        }

        public static bool operator >=(LogIndex x, LogIndex y)
        {
            var res = x.CompareTo(y);

            return res > 0 || res == 0;
        }

        public static bool operator <(LogIndex x, LogIndex y)
        {
            return x.CompareTo(y) < 0;
        }

        public static bool operator >(LogIndex x, LogIndex y)
        {
            return x.CompareTo(y) > 0;
        }

        protected bool Equals(LogIndex other)
        {
            return Index == other.Index;
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
            return Equals((LogIndex) obj);
        }

        public override int GetHashCode()
        {
            return Index.GetHashCode();
        }

        public int CompareTo(object obj)
        {
            return Index.CompareTo(((LogIndex) obj).Index);
        }

        public ILogIndex Increment()
        {
            return new LogIndex(Index + 1);
        }

        public ILogIndex Dicrement()
        {
            return new LogIndex(Index - 1);
        }

        public ulong Index { get; private set; }
    }
}