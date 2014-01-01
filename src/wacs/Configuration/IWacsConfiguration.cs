namespace wacs.Configuration
{
    public interface IWacsConfiguration
    {
        INodeResolverConfiguration NodeResolver { get; }
        ITopologyConfiguration Topology { get; }
        ILeaseConfiguration Lease { get; }
    }
}