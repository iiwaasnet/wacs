using System;
using System.Collections.Concurrent;
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
        private ConcurrentDictionary<Configuration.INode, object> world;
        private ConcurrentDictionary<Configuration.INode, object> synod;
        private readonly INode localNode;
        private readonly object locker = new object();

        public SynodConfigurationProvider(ISynodConfiguration config)
        {
            eventHandlers = new EventHandlerList();
            var dictionary = config.Nodes.ToDictionary(n => (Configuration.INode) new Endpoint(n), n => (object) null);

            world = new ConcurrentDictionary<Configuration.INode, object>(dictionary);
            synod = new ConcurrentDictionary<Configuration.INode, object>(dictionary);
            localNode = new Node();
        }

        public void ActivateNewSynod(IEnumerable<Configuration.INode> newSynod)
        {
            lock (locker)
            {
                var tmpSynod = newSynod.ToDictionary(n => (Configuration.INode) new Endpoint(n), n => (object) null);

                var tmpWorld = MergeNewSynodAndRemainedWorld(tmpSynod, RemoveOldSynodFromWorld());

                synod = new ConcurrentDictionary<Configuration.INode, object>(tmpSynod);
                world = new ConcurrentDictionary<Configuration.INode, object>(tmpWorld);

                OnSynodChanged();
                OnWorldChanged();
            }
        }

        private IEnumerable<KeyValuePair<Configuration.INode, object>> MergeNewSynodAndRemainedWorld(Dictionary<Configuration.INode, object> tmpSynod, IDictionary<Configuration.INode, object> tmpWorld)
        {
            foreach (var node in tmpSynod)
            {
                tmpWorld[node.Key] = null;
            }

            return tmpWorld;
        }

        private IDictionary<Configuration.INode, object> RemoveOldSynodFromWorld()
        {
            return world
                .Where(pair => !synod.ContainsKey(pair.Key))
                .ToDictionary(pair => pair.Key, pair => pair.Value);
        }

        public void AddNodeToWorld(Configuration.INode newNode)
        {
            lock (locker)
            {
                if (world.TryAdd(newNode, null))
                {
                    OnWorldChanged();
                }
            }
        }

        public void DetachNodeFromWorld(Configuration.INode detachedNode)
        {
            lock (locker)
            {
                AssertNodeIsNotInSynode(detachedNode);

                object value;
                if (world.TryRemove(detachedNode, out value))
                {
                    OnWorldChanged();
                }
            }
        }

        private void AssertNodeIsNotInSynode(Configuration.INode detachedNode)
        {
            if (synod.ContainsKey(detachedNode))
            {
                throw new Exception(string.Format("Unable to detach node from world! Node {0} is part of the synod.", detachedNode.Address));
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

        public class Endpoint : Configuration.INode
        {
            internal Endpoint(Configuration.INode node)
            {
                Address = node.NormalizeEndpointAddress();
                IsLocal = node.IsLocal;
            }

            public Endpoint(string uri)
            {
                Address = uri.NormalizeEndpointAddress();
                IsLocal = false;
            }

            protected bool Equals(Endpoint other)
            {
                return string.Equals(Address, other.Address);
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj))
                {
                    return false;
                }
                if (ReferenceEquals(this, obj))
                {
                    return true;
                }
                if (obj.GetType() != this.GetType())
                {
                    return false;
                }
                return Equals((Endpoint) obj);
            }

            public override int GetHashCode()
            {
                return (Address != null ? Address.GetHashCode() : 0);
            }

            public string Address { get; private set; }
            public bool IsLocal { get; private set; }
        }
    }
}