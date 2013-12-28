using System.Collections.Generic;
using wacs.Configuration;

namespace wacs.Paxos.Interface
{
    public delegate void WorldChangedHandler();

    public interface ISynodConfigurationProvider
    {
        void ActivateNewSynod(IEnumerable<IEndpoint> newSynod);

        void AddNodeToWorld(IEndpoint newEndpoint);

        void DetachNodeFromWorld(IEndpoint detachedEndpoint);

        event WorldChangedHandler WorldChanged;

        event WorldChangedHandler SynodChanged;

        IEnumerable<IEndpoint> World { get; }
        IEnumerable<IEndpoint> Synod { get; }
        INode LocalNode { get; }
        IEndpoint LocalEndpoint { get; }
    }
}