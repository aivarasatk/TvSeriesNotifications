using System.Collections.Generic;

namespace TVSeriesNotifications.Persistance
{
    public interface IPersistantCache<T>
    {
        /// <summary>
        /// If key exists in the cache the value is set and true is retured, otherwise returns false and default value is set.
        /// </summary>
        /// <returns>True if value exists, false otherwise.</returns>
        bool TryGet(string key, out T value);

        IEnumerable<string> Keys();

        bool Exists(string key);

        void Add(string key, T value);

        void Update(string key, T value);

        void Remove(string key);
    }
}
