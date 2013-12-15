using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using wacs.Configuration;
using wacs.core;
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
            world = config.Nodes.Select(n => new Endpoint(n));
            synod = config.Nodes.Select(n => new Endpoint(n));
            localNode = new Node();
        }

        public void NewSynod(IEnumerable<Configuration.INode> newSynod)
        {
            OnSynodChanged();
        }

        public void AddNodeToWorld(Configuration.INode newNodes)
        {
            OnWorldChanged();
        }

        private void OnWorldChanged()
        {
            var handler = eventHandlers[WorldChangedEvent] as WorldChangedHandler;
            if (handler != null)
            {
                handler();
            }
        }

        private void OnSynodChanged()
        {
            var handler = eventHandlers[SynodChangedEvent] as WorldChangedHandler;
            if (handler != null)
            {
                handler();
            }
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

        private class Endpoint : Configuration.INode
        {
            public Endpoint(Configuration.INode node)
            {
                Address = node.NormalizeEndpointAddress();
                IsLocal = node.IsLocal;
            }

            public string Address { get; private set; }
            public bool IsLocal { get; private set; }
        }
    }
}