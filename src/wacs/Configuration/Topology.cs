namespace wacs.Configuration
{
    public class Topology : ITopologyConfiguration
    {
        public INode LocalNode { get; set; }

        public ISynod Synod { get; set; }
    }
}