using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TVSeriesNotifications.Persistance
{
    public interface IPersistantCache
    {
        /// <summary>
        /// If key exists in the cache the value is set and true is retured, otherwise returns false and default value is set
        /// </summary>
        /// <typeparam name="T">Any object</typeparam>
        /// <returns>True if value exists, false otherwise</returns>
        bool TryGet<T>(string key, out T value);

        IEnumerable<string> Keys();

        bool Exists(string key);

        void Add<T>(string key, T value);

        void Update<T>(string key, T value);

        void Remove(string key);
    }
}
