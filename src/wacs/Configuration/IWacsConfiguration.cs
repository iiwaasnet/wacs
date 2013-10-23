namespace wacs.Configuration
{
    public interface IWacsConfiguration
    {
        int FarmSize { get; }
        ISynodConfiguration Synod { get; }

        ILeaseConfiguration Lease { get; }
    }
}