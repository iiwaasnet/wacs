using wacs.Resolver.Interface;

namespace wacs.Configuration
{
    public interface IWacsConfiguration
    {
        IHostResolverConfiguration HostResolver { get; }

        int FarmSize { get; }
        ISynodConfiguration Synod { get; }

        ILeaseConfiguration Lease { get; }
    }
}