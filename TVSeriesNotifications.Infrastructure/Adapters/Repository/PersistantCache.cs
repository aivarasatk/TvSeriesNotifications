using System.Runtime.Caching;
using TVSeriesNotifications.Domain.Ports.Repository;

namespace TVSeriesNotifications.Persistance
{
    public class PersistantCache<T> : IPersistantCache<T>
    {
        private FileCache _fileCache;

        public PersistantCache(string cacheLocation)
        {
            _fileCache = new FileCache(Path.Combine(Directory.GetCurrentDirectory(), cacheLocation));
        }

        public void Add(string key, T value)
        {
            lock (_fileCache)
                _fileCache.Add(key, value, DateTimeOffset.Now.AddYears(10));
        }

        public void Update(string key, T value)
        {
            lock (_fileCache)
                _fileCache.Set(key, value, DateTimeOffset.Now.AddYears(10));
        }

        public bool TryGet(string key, out T value)
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
