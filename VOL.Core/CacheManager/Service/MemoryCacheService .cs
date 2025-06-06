
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Linq;

namespace VOL.Core.CacheManager
{
    /// <summary>
    /// Implements the <see cref="ICacheService"/> interface using <see cref="IMemoryCache"/> for in-memory caching.
    /// This service ensures that cache entries have default expiration policies and contribute to a global size limit.
    /// </summary>
    public class MemoryCacheService : ICacheService
    {
        /// <summary>
        /// The underlying IMemoryCache instance.
        /// </summary>
        protected IMemoryCache _cache;

        /// <summary>
        /// Initializes a new instance of the <see cref="MemoryCacheService"/> class.
        /// </summary>
        /// <param name="cache">The IMemoryCache instance to be used for caching operations.</param>
        public MemoryCacheService(IMemoryCache cache)
        {
            _cache = cache;
        }

        /// <summary>
        /// Verifies if a cache entry exists for the given key.
        /// </summary>
        /// <param name="key">The cache key to check.</param>
        /// <returns><c>true</c> if the key exists in the cache; otherwise, <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException">Thrown if the key is null.</exception>
        public bool Exists(string key)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }
            return _cache.Get(key) != null;
        }

        /// <summary>
        /// Adds an object to the cache with a default absolute expiration of 1 hour and a default size of 1.
        /// </summary>
        /// <param name="key">The cache key for the value.</param>
        /// <param name="value">The object to cache.</param>
        /// <returns><c>true</c> if the item was successfully added (or already existed and was updated); otherwise, <c>false</c> (though this implementation always returns based on Exists check after Set).</returns>
        /// <exception cref="ArgumentNullException">Thrown if key or value is null.</exception>
        public bool Add(string key, object value)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }
            var options = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(TimeSpan.FromHours(1)) // Default 1-hour absolute expiration
                .SetSize(1); // Set entry size to 1 for global cache size limit calculation
            _cache.Set(key, value, options);
            return Exists(key); // Confirms the item is in cache
        }

        /// <summary>
        /// Adds an object to the cache with configurable expiration.
        /// If <paramref name="expireSeconds"/> is -1, a default absolute expiration of 1 hour is applied.
        /// Otherwise, uses the specified <paramref name="expireSeconds"/> for either sliding or absolute expiration.
        /// All entries are added with a default size of 1.
        /// </summary>
        /// <param name="key">The cache key for the value.</param>
        /// <param name="value">The object to cache.</param>
        /// <param name="expireSeconds">Expiration in seconds. If -1, a default 1-hour absolute expiration is used. Otherwise, this value is used for sliding or absolute expiration.</param>
        /// <param name="isSliding">If true and <paramref name="expireSeconds"/> is positive, sets a sliding expiration. If false and <paramref name="expireSeconds"/> is positive, sets an absolute expiration.</param>
        /// <returns>Always returns <c>true</c> after attempting to set the cache item.</returns>
        public bool AddObject(string key, object value, int expireSeconds = -1, bool isSliding = false)
        {
            MemoryCacheEntryOptions options = new MemoryCacheEntryOptions();
            options.SetSize(1); // Set entry size to 1 for global cache size limit calculation

            if (expireSeconds != -1) // Specific expiration is provided
            {
                if (isSliding)
                {
                    options.SetSlidingExpiration(TimeSpan.FromSeconds(expireSeconds));
                }
                else
                {
                    options.SetAbsoluteExpiration(TimeSpan.FromSeconds(expireSeconds));
                }
            }
            else // No specific expiration time provided (expireSeconds is -1)
            {
                // Apply default absolute expiration of 1 hour
                options.SetAbsoluteExpiration(TimeSpan.FromHours(1));
            }
            _cache.Set(key, value, options);
            return true;
        }

        /// <summary>
        /// Adds a string value to the cache by calling <see cref="AddObject"/>.
        /// Inherits the expiration and sizing logic from <see cref="AddObject"/>.
        /// </summary>
        /// <param name="key">The cache key for the value.</param>
        /// <param name="value">The string value to cache.</param>
        /// <param name="expireSeconds">Expiration in seconds. See <see cref="AddObject"/> for details.</param>
        /// <param name="isSliding">Whether to use sliding expiration. See <see cref="AddObject"/> for details.</param>
        /// <returns>The result of the <see cref="AddObject"/> call.</returns>
        public bool Add(string key, string value, int expireSeconds = -1, bool isSliding = false)
        {
            return AddObject(key, value, expireSeconds, isSliding);
        }

        // The following List operations are not implemented for IMemoryCache by default.
        // They are likely stubs for a Redis-backed ICacheService or similar.
        // No IDisposable resources are managed here.
        public void LPush(string key, string val) { /* Not implemented for MemoryCache */ }
        public void RPush(string key, string val) { /* Not implemented for MemoryCache */ }
        public T ListDequeue<T>(string key) where T : class { return null; /* Not implemented */ }
        public object ListDequeue(string key) { return null; /* Not implemented */ }
        public void ListRemove(string key, int keepIndex) { /* Not implemented */ }

        /// <summary>
        /// Adds an object to the cache with specified sliding and absolute expiration times.
        /// The entry size is set to 1.
        /// </summary>
        /// <param name="key">The cache key.</param>
        /// <param name="value">The object to cache.</param>
        /// <param name="expiresSliding">Sliding expiration time. The cache entry expires if it hasn't been accessed for this duration.</param>
        /// <param name="expiressAbsoulte">Absolute expiration time. The cache entry expires after this duration, regardless of activity.</param>
        /// <returns><c>true</c> if the item was successfully added; otherwise, <c>false</c> (based on subsequent Exists check).</returns>
        public bool Add(string key, object value, TimeSpan expiresSliding, TimeSpan expiressAbsoulte)
        {
            _cache.Set(key, value,
                    new MemoryCacheEntryOptions()
                    .SetSlidingExpiration(expiresSliding)
                    .SetAbsoluteExpiration(expiressAbsoulte)
                    .SetSize(1) // Set entry size
                    );
            return Exists(key);
        }

        /// <summary>
        /// Adds an object to the cache with a specified expiration duration, optionally as sliding expiration.
        /// The entry size is set to 1.
        /// </summary>
        /// <param name="key">The cache key.</param>
        /// <param name="value">The object to cache.</param>
        /// <param name="expiresIn">The duration for which the item should be cached.</param>
        /// <param name="isSliding">If true, sets a sliding expiration; otherwise, sets an absolute expiration.</param>
        /// <returns><c>true</c> if the item was successfully added; otherwise, <c>false</c> (based on subsequent Exists check).</returns>
        public bool Add(string key, object value, TimeSpan expiresIn, bool isSliding = false)
        {
            MemoryCacheEntryOptions options = new MemoryCacheEntryOptions().SetSize(1); // Set entry size
            if (isSliding)
                _cache.Set(key, value, options.SetSlidingExpiration(expiresIn));
            else
                _cache.Set(key, value, options.SetAbsoluteExpiration(expiresIn));

            return Exists(key);
        }

        /// <summary>
        /// Removes a cache entry for the given key.
        /// </summary>
        /// <param name="key">The cache key to remove.</param>
        /// <returns><c>true</c> if the item was removed (or didn't exist); otherwise, <c>false</c> (this implementation returns !Exists).</returns>
        /// <exception cref="ArgumentNullException">Thrown if the key is null.</exception>
        public bool Remove(string key)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }
            _cache.Remove(key);
            return !Exists(key); // Returns true if item no longer exists
        }

        /// <summary>
        /// Removes all cache entries for the specified keys.
        /// </summary>
        /// <param name="keys">A collection of cache keys to remove.</param>
        /// <exception cref="ArgumentNullException">Thrown if the keys collection is null.</exception>
        public void RemoveAll(IEnumerable<string> keys)
        {
            if (keys == null)
            {
                throw new ArgumentNullException(nameof(keys));
            }
            keys.ToList().ForEach(item => _cache.Remove(item));
        }

        /// <summary>
        /// Gets a string value from the cache for the given key.
        /// </summary>
        /// <param name="key">The cache key.</param>
        /// <returns>The cached string value, or null if the key is not found or the value is not a string.</returns>
        public string Get(string key)
        {
            return _cache.Get(key)?.ToString();
        }

        /// <summary>
        /// Gets an object of type <typeparamref name="T"/> from the cache for the given key.
        /// </summary>
        /// <typeparam name="T">The type of the cached object.</typeparam>
        /// <param name="key">The cache key.</param>
        /// <returns>The cached object, or null if the key is not found or the object cannot be cast to <typeparamref name="T"/>.</returns>
        /// <exception cref="ArgumentNullException">Thrown if the key is null.</exception>
        public T Get<T>(string key) where T : class
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }
            return _cache.Get(key) as T;
        }

        /// <summary>
        /// Disposes the underlying <see cref="IMemoryCache"/> instance if it's disposable.
        /// </summary>
        public void Dispose()
        {
            if (_cache != null)
                _cache.Dispose(); // IMemoryCache itself is IDisposable
            GC.SuppressFinalize(this);
        }
    }
}
