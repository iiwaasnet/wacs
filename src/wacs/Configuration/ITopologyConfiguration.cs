namespace wacs.Configuration
{
    public interface ITopologyConfiguration
    {
        INode LocalNode { get; }
        ISynod Synod { get; }
    }
}