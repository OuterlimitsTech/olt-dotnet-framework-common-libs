using System;
using System.Runtime.Caching;

namespace OLT.Core
{
    public class OltMemoryCache : OltMemoryCacheBase
    {
        /// <summary>
        /// A generic method for getting and setting objects to the memory cache.
        /// </summary>
        /// <typeparam name="TEntry">The type of the object to be returned.</typeparam>
        /// <param name="key">The name to be used when storing this object in the cache.</param>
        /// <param name="absoluteExpiration">When to expire the object to cache this object for.</param>
        /// <param name="factory">A parameterless function to call if the object isn't in the cache and you need to set it.</param>
        /// <returns>An object of the type you asked for</returns>
        public override TEntry Get<TEntry>(string key, DateTimeOffset absoluteExpiration, Func<TEntry> factory)
        {
            ObjectCache cache = MemoryCache.Default;
            var cachedObject = (TEntry)cache[key];
            if (cachedObject == null)
            {
                CacheItemPolicy policy = new CacheItemPolicy
                {
                    AbsoluteExpiration = absoluteExpiration
                };
                cachedObject = factory();
                cache.Set(key, cachedObject, policy);
            }
            return cachedObject;
        }


        /// <summary>
        /// A generic method for getting and setting objects to the memory cache.
        /// </summary>
        /// <param name="key">The name to be used for this object in the cache.</param>
        public override void Remove(string key)
        {
            ObjectCache cache = MemoryCache.Default;
            var cachedObject = cache[key];
            if (cachedObject != null)
            {
                cache.Remove(key);
            }
        }
    }
}