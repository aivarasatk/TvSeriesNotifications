using System;
using System.Collections.Generic;
using System.Runtime.Caching;

namespace TVSeriesNotifications.Persistance
{
    public class PersistantCache : IPersistantCache
    {
        private FileCache _fileCache;

        public PersistantCache(string cacheLocation)
        {
            _fileCache = new FileCache(cacheLocation);
        }

        public void Add<T>(string key, T value)
        {
            lock (_fileCache)
                _fileCache.Add(key, value, DateTimeOffset.Now.AddYears(10));
        }

        public void Update<T>(string key, T value)
        {
            lock (_fileCache)
                _fileCache.Set(key, value, DateTimeOffset.Now.AddYears(10));
        }

        public bool TryGet<T>(string key, out T value)
        {
            lock (_fileCache)
            {
                if (_fileCache.Contains(key))
                {
                    value = (T)_fileCache.Get(key);
                    return true;
                }
            }

            value = default;
            return false;
        }

        public void Remove(string key)
        {
            lock (_fileCache)
                _fileCache.Remove(key);
        }

        public bool Exists(string key)
        {
            lock (_fileCache)
                return _fileCache.Contains(key);
        }

        public IEnumerable<string> Keys()
        {
            lock (_fileCache)
                return _fileCache.GetKeys();
        }
    }
}
