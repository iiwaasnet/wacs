using wacs.core;

namespace wacs
{
    public class Node : INode
    {
        public Node()
        {
            Id = UniqueIdGenerator.Generate(3);
        }

        public Node(int id)
        {
            Id = id;
        }

        protected bool Equals(Node other)
        {
            return Id == other.Id;
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
            return Equals((Node) obj);
        }

        public override int GetHashCode()
        {
            return Id;
        }

        public int Id { get; private set; }

        public static bool operator ==(Node x, Node y)
        {
            return x.Equals(y);
        }

        public static bool operator !=(Node x, Node y)
        {
            return !(x == y);
        }
    }
}