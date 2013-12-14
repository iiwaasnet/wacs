using System.Collections.Generic;
using System.ComponentModel;
using wacs.Configuration;
using wacs.Paxos.Interface;

namespace wacs.Paxos.Implementation
{
    public class SynodConfigurationProvider : ISynodConfigurationProvider
    {
        private readonly EventHandlerList eventHandlers;
        private static readonly object WorldChangedEvent = new object();
        private static readonly object SynodChangedEvent = new object();
        private readonly IEnumerable<Configuration.INode> world;
        private readonly IEnumerable<Configuration.INode> synod;
        private readonly INode localNode;

        public SynodConfigurationProvider(ISynodConfiguration config)
        {
            eventHandlers = new EventHandlerList();
            world = config.Nodes;
            synod = config.Nodes;
            localNode = new Node();
        }

        public event WorldChangedHandler WorldChanged
        {
            add { eventHandlers.AddHandler(WorldChangedEvent, value); }
            remove { eventHandlers.RemoveHandler(WorldChangedEvent, value); }
        }

        public event WorldChangedHandler SynodChanged
        {
            add { eventHandlers.AddHandler(SynodChangedEvent, value); }
            remove { eventHandlers.RemoveHandler(SynodChangedEvent, value); }
        }

        public IEnumerable<Configuration.INode> World
        {
            get { return world; }
        }

        public IEnumerable<Configuration.INode> Synod
        {
            get { return synod; }
        }

        public INode LocalNode
        {
            get { return localNode; }
        }
    }
}