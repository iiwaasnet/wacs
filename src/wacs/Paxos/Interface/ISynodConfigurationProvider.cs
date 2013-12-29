﻿using System.Collections.Generic;
using wacs.Configuration;

namespace wacs.Paxos.Interface
{
    public delegate void WorldChangedHandler();

    public interface ISynodConfigurationProvider
    {
        void ActivateNewSynod(IEnumerable<INode> newSynod);

        void AddNodeToWorld(INode newNode);

        void DetachNodeFromWorld(INode detachedNode);

        event WorldChangedHandler WorldChanged;

        event WorldChangedHandler SynodChanged;

        IEnumerable<INode> World { get; }
        IEnumerable<INode> Synod { get; }
        IProcess LocalProcess { get; }
        INode LocalNode { get; }
    }
}