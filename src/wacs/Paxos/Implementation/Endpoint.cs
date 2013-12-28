using System;
using wacs.Configuration;

namespace wacs.Paxos.Implementation
{
    public class Endpoint : IEndpoint
    {
        public Endpoint(IEndpoint endpoint)
        {
            Address = NormalizeEndpointAddress(endpoint.Address);
        }

        public Endpoint(string uri)
        {
            Address = NormalizeEndpointAddress(uri);
        }

        private static string NormalizeEndpointAddress(string uri)
        {
            return new Uri(uri).AbsoluteUri.TrimEnd('/');
        }

        protected bool Equals(Endpoint other)
        {
            return String.Equals(Address, other.Address);
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
            return Equals((Endpoint) obj);
        }

        public override int GetHashCode()
        {
            return (Address != null ? Address.GetHashCode() : 0);
        }

        public string Address { get; private set; }
    }
}