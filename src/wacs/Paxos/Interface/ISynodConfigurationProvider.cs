using System.Collections.Generic;

namespace wacs.Paxos.Interface
{
    public delegate void WorldChangedHandler();
    public interface ISynodConfigurationProvider
    {
        void NewSynod(IEnumerable<Configuration.INode> newSynod);
        void AddNodeToWorld(Configuration.INode newNode);
        void DetachNodeFromWorld(Configuration.INode detachedNode);
        IEnumerable<Configuration.INode> World { get; }
        IEnumerable<Configuration.INode> Synod { get; }
        INode LocalNode { get; }

        event WorldChangedHandler WorldChanged;

        event WorldChangedHandler SynodChanged;
    }
}