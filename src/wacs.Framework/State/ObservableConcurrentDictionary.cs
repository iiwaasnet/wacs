using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;

namespace wacs.Framework.State
{
    public class ObservableConcurrentDictionary<K, V> : IObservableConcurrentDictionary<K, V>
    {
        private ConcurrentDictionary<K, V> storage;
        private readonly EventHandlerList eventHandlers;
        private readonly object ChangedEvent = new object();

        public ObservableConcurrentDictionary()
        {
            eventHandlers = new EventHandlerList();
            storage = new ConcurrentDictionary<K, V>();
        }

        public ObservableConcurrentDictionary(IEnumerable<KeyValuePair<K, V>> collection)
        {
            eventHandlers = new EventHandlerList();
            storage = new ConcurrentDictionary<K, V>(collection);
        }

        public int Count()
        {
            //test
            return storage.Count;
        }

        public void Set(IEnumerable<KeyValuePair<K, V>> collection)
        {
            Interlocked.Exchange(ref storage, new ConcurrentDictionary<K, V>(collection));
            OnChanged();
        }

        public bool TryGetValue(K key, out V val)
        {
            return storage.TryGetValue(key, out val);
        }

        public bool TryRemoveKey(K key)
        {
            V val;
            var res = storage.TryRemove(key, out val);
            if (res)
            {
                OnChanged();
            }

            return res;
        }

        private void OnChanged()
        {
            var handler = eventHandlers[ChangedEvent] as ChangedEventHandler;

            if (handler != null)
            {
                handler();
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public IEnumerator<KeyValuePair<K, V>> GetEnumerator()
        {
            return storage.GetEnumerator();
        }

        public bool ContainsKey(K key)
        {
            return storage.ContainsKey(key);
        }

        public event ChangedEventHandler Changed
        {
            add { eventHandlers.AddHandler(ChangedEvent, value); }
            remove { eventHandlers.RemoveHandler(ChangedEvent, value); }
        }

        public IEnumerable<V> Values
        {
            get { return storage.Values; }
        }

        public IEnumerable<K> Keys
        {
            get { return storage.Keys; }
        }

        public V this[K key]
        {
            get { return storage[key]; }
            set
            {
                storage[key] = value;
                OnChanged();
            }
        }
    }
}