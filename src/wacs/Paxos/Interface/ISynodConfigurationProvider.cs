using System.Collections.Generic;

namespace wacs.Paxos.Interface
{
    public delegate void WorldChangedHandler();
    public interface ISynodConfigurationProvider
    {
        IEnumerable<Configuration.INode> World { get; }
        INode LocalNode { get; }

        event WorldChangedHandler WorldChanged;
    }
}