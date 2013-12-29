namespace wacs.Configuration
{
    public interface IWacsConfiguration
    {
        IHostResolverConfiguration HostResolver { get; }
        ITopologyConfiguration Topology { get; }
        ILeaseConfiguration Lease { get; }
    }
}