using System.Collections.Generic;
using TVSeriesNotifications.Domain.Ports.Repository;

namespace TVSeriesNotifications.Tests.Fakes.Persistance
{
    public class FakePersistantCache<T> : IPersistantCache<T>
    {
        public Dictionary<string, T> CacheItems { get; set; } = new Dictionary<string, T>();

        public FakePersistantCache()
        {
        }

        public FakePersistantCache(Dictionary<string, T> defaultCacheItemList)
        {
            CacheItems = new Dictionary<string, T>(defaultCacheItemList);
        }

        public void Add(string key, T value)
        {
            CacheItems.Add(key, value);
        }

        public bool Exists(string key)
        {
            return CacheItems.ContainsKey(key);
        }

        public IEnumerable<string> Keys()
        {
            return CacheItems.Keys;
        }

        public void Remove(string key)
        {
            CacheItems.Remove(key);
        }

        public bool TryGet(string key, out T value)
        {
            var success = CacheItems.TryGetValue(key, out value);
            return success;

        }

        public void Update(string key, T value)
        {
            if (CacheItems.ContainsKey(key))
                CacheItems[key] = value;
            else
                CacheItems.Add(key, value);
        }
    }
}
