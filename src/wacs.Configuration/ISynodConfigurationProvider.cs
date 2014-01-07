using System.Collections.Generic;

namespace wacs.Configuration
{
    public delegate void WorldChangedHandler();

    public interface ISynodConfigurationProvider
    {
        void ActivateNewSynod(IEnumerable<INode> newSynod);

        void AddNodeToWorld(INode newNode);

        void DetachNodeFromWorld(INode detachedNode);

        bool IsMemberOfSynod(INode node);

        event WorldChangedHandler WorldChanged;

        event WorldChangedHandler SynodChanged;

        IEnumerable<INode> World { get; }
        IEnumerable<INode> Synod { get; }
        IProcess LocalProcess { get; }
        INode LocalNode { get; }
    }
}