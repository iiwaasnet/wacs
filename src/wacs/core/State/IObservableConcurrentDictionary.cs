﻿using System.Collections.Generic;

namespace wacs.core.State
{
    public interface IObservableConcurrentDictionary<K, V> : IChangeNotifiable, IEnumerable<KeyValuePair<K, V>>
    {
        int Count();

        bool ContainsKey(K key);

        void Set(IEnumerable<KeyValuePair<K, V>> collection);

        IEnumerable<V> Values { get; }

        V this[K key] { get; set; }
    }
}