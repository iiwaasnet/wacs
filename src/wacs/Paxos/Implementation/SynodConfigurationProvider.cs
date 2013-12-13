using System.Collections.Generic;
using System.ComponentModel;
using wacs.Configuration;
using wacs.Paxos.Interface;

namespace wacs.Paxos.Implementation
{
    internal class SynodConfigurationProvider : ISynodConfigurationProvider
    {
        private readonly EventHandlerList eventHandlers;
        private static readonly object WorldChangedEvent = new object();
        private readonly IEnumerable<Configuration.INode> world;
        private readonly INode localNode;

        public SynodConfigurationProvider(ISynodConfiguration config)
        {
            eventHandlers = new EventHandlerList();
            world = config.Nodes;
            localNode = new Node();
        }

        public event WorldChangedHandler WorldChanged
        {
            add { eventHandlers.AddHandler(WorldChangedEvent, value); }
            remove { eventHandlers.RemoveHandler(WorldChangedEvent, value); }
        }

        public IEnumerable<Configuration.INode> World
        {
            get { return world; }
        }

        public INode LocalNode
        {
            get { return localNode; }
        }
    }
}