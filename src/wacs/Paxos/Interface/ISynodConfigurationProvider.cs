using System.Collections.Generic;

namespace wacs.Paxos.Interface
{
    public delegate void WorldChangedHandler();
    public interface ISynodConfigurationProvider
    {
        void ChangeSynod(IEnumerable<Configuration.INode> newSynod);
        void ChangeWorld(IEnumerable<Configuration.INode> newWorld);
        IEnumerable<Configuration.INode> World { get; }
        IEnumerable<Configuration.INode> Synod { get; }
        INode LocalNode { get; }

        event WorldChangedHandler WorldChanged;

        event WorldChangedHandler SynodChanged;
    }
}