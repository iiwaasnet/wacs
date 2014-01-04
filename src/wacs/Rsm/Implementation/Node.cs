using System;
using wacs.Configuration;

namespace wacs.Rsm.Implementation
{
    public class Node : INode
    {
        public Node(INode node)
        {
            Address = NormalizeEndpointAddress(node.Address);
        }

        public Node(string uri)
        {
            Address = NormalizeEndpointAddress(uri);
        }

        private static string NormalizeEndpointAddress(string uri)
        {
            return new Uri(uri).AbsoluteUri.TrimEnd('/');
        }

        protected bool Equals(Node other)
        {
            return string.Equals(Address, other.Address);
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
            return (Address != null ? Address.GetHashCode() : 0);
        }

        public string Address { get; private set; }
    }
}