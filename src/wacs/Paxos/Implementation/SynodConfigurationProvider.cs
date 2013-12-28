using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using wacs.Configuration;
using wacs.Paxos.Interface;

namespace wacs.Paxos.Implementation
{
    public class SynodConfigurationProvider : ISynodConfigurationProvider
    {
        private readonly EventHandlerList eventHandlers;
        private static readonly object WorldChangedEvent = new object();
        private static readonly object SynodChangedEvent = new object();
        private ConcurrentDictionary<IEndpoint, object> world;
        private ConcurrentDictionary<IEndpoint, object> synod;
        private readonly INode localNode;
        private readonly IEndpoint localEndpoint;
        private readonly object locker = new object();

        public SynodConfigurationProvider(ISynodConfiguration config)
        {
            eventHandlers = new EventHandlerList();

            localNode = new Node();
            localEndpoint = new Endpoint(config.LocalNode);
            var dictionary = config.Members.ToDictionary(n => (IEndpoint) new Endpoint(n), n => (object) null);

            AssertNotEmptySynodIncludesLocalNode(dictionary.Keys);

            synod = new ConcurrentDictionary<IEndpoint, object>(dictionary);
            world = new ConcurrentDictionary<IEndpoint, object>(dictionary);
            EnsureInitialWorldIncludesLocalNode(world, localEndpoint);
        }

        private void EnsureInitialWorldIncludesLocalNode(ConcurrentDictionary<IEndpoint, object> world, IEndpoint localEndpoint)
        {
            world.TryAdd(localEndpoint, null);
        }

        private void AssertNotEmptySynodIncludesLocalNode(IEnumerable<IEndpoint> synod)
        {
            if (synod != null && synod.Any() && !synod.Any(ep => ep.Address == localEndpoint.Address))
            {
                throw new Exception(string.Format("Synod should be empty or include local node {0}!", localEndpoint.Address));
            }
        }

        public void ActivateNewSynod(IEnumerable<IEndpoint> newSynod)
        {
            lock (locker)
            {
                var tmpSynod = newSynod.ToDictionary(n => (IEndpoint) new Endpoint(n), n => (object) null);

                var tmpWorld = MergeNewSynodAndRemainedWorld(tmpSynod, RemoveOldSynodFromWorld());

                synod = new ConcurrentDictionary<IEndpoint, object>(tmpSynod);
                world = new ConcurrentDictionary<IEndpoint, object>(tmpWorld);

                OnSynodChanged();
                OnWorldChanged();
            }
        }

        private IEnumerable<KeyValuePair<IEndpoint, object>> MergeNewSynodAndRemainedWorld(Dictionary<IEndpoint, object> tmpSynod,
                                                                                           IDictionary<IEndpoint, object> tmpWorld)
        {
            foreach (var node in tmpSynod)
            {
                tmpWorld[node.Key] = null;
            }

            return tmpWorld;
        }

        private IDictionary<IEndpoint, object> RemoveOldSynodFromWorld()
        {
            return world
                .Where(pair => !synod.ContainsKey(pair.Key))
                .ToDictionary(pair => pair.Key, pair => pair.Value);
        }

        public void AddNodeToWorld(IEndpoint newEndpoint)
        {
            lock (locker)
            {
                if (world.TryAdd(newEndpoint, null))
                {
                    OnWorldChanged();
                }
            }
        }

        public void DetachNodeFromWorld(IEndpoint detachedEndpoint)
        {
            lock (locker)
            {
                AssertNodeIsNotInSynode(detachedEndpoint);

                object value;
                if (world.TryRemove(detachedEndpoint, out value))
                {
                    OnWorldChanged();
                }
            }
        }

        private void AssertNodeIsNotInSynode(IEndpoint detachedEndpoint)
        {
            if (synod.ContainsKey(detachedEndpoint))
            {
                throw new Exception(string.Format("Unable to detach node from world! Node {0} is part of the synod.", detachedEndpoint.Address));
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

        public IEnumerable<IEndpoint> World
        {
            get { return world.Keys; }
        }

        public IEnumerable<IEndpoint> Synod
        {
            get { return synod.Keys; }
        }

        public INode LocalNode
        {
            get { return localNode; }
        }

        public IEndpoint LocalEndpoint
        {
            get { return localEndpoint; }
        }
    }
}