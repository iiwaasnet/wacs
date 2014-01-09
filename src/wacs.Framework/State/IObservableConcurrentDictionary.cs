using System.Collections.Generic;

namespace wacs.Framework.State
{
    public interface IObservableConcurrentDictionary<K, V> : IChangeNotifiable, IEnumerable<KeyValuePair<K, V>>
    {
        int Count();

        bool ContainsKey(K key);

        bool TryRemoveKey(K key);

        bool TryGetValue(K key, out V val);

        void Set(IEnumerable<KeyValuePair<K, V>> collection);

        IEnumerable<V> Values { get; }
        IEnumerable<K> Keys { get; }

        V this[K key] { get; set; }
    }
}