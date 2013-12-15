using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
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
        private readonly ConcurrentDictionary<Configuration.INode, object> world;
        private volatile ConcurrentDictionary<Configuration.INode, object> synod;
        private readonly INode localNode;

        public SynodConfigurationProvider(ISynodConfiguration config)
        {
            eventHandlers = new EventHandlerList();
            var dictionary = config.Nodes.ToDictionary(n => (Configuration.INode) new Endpoint(n), n => new object());

            world = new ConcurrentDictionary<Configuration.INode, object>(dictionary);
            synod = new ConcurrentDictionary<Configuration.INode, object>(dictionary);
            localNode = new Node();
        }

        public void NewSynod(IEnumerable<Configuration.INode> newSynod)
        {
            var dictionary = newSynod.ToDictionary(n => (Configuration.INode) new Endpoint(n), n => new object());
            Interlocked.Exchange(ref synod, new ConcurrentDictionary<Configuration.INode, object>(dictionary));

            OnSynodChanged();
            OnWorldChanged();
        }

        public void AddNodeToWorld(Configuration.INode newNode)
        {
            if (world.TryAdd(newNode, null))
            {
                OnWorldChanged();
            }
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
            get { return world.Keys; }
        }

        public IEnumerable<Configuration.INode> Synod
        {
            get { return synod.Keys; }
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