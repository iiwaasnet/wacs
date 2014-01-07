namespace wacs.Configuration
{
    public class Node : INode
    {
        private readonly string intercomAddress;
        private readonly string serviceAddress;

        public Node(INode node)
            : this(node.BaseAddress, node.IntercomPort, node.ServicePort)
        {
        }

        public Node(string baseAddress, int intercomPort, int servicePort)
        {
            BaseAddress = baseAddress.TrimEnd('/');
            IntercomPort = intercomPort;
            ServicePort = servicePort;

            intercomAddress = CreateEndpointAddress(BaseAddress, IntercomPort);
            serviceAddress = CreateEndpointAddress(BaseAddress, IntercomPort);
        }

        private string CreateEndpointAddress(string baseAddress, int port)
        {
            return string.Format("{0}:{1}", baseAddress, port);
        }

        public string GetServiceAddress()
        {
            return serviceAddress;
        }

        public string GetIntercomAddress()
        {
            return intercomAddress;
        }

        protected bool Equals(Node other)
        {
            return string.Equals(GetIntercomAddress(), other.GetIntercomAddress());
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
            unchecked
            {
                return (!string.IsNullOrWhiteSpace(GetIntercomAddress()) ? GetIntercomAddress().GetHashCode() : 0);
            }
        }

        public string BaseAddress { get; private set; }
        public int IntercomPort { get; private set; }
        public int ServicePort { get; private set; }
    }
}