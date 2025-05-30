using System;
using System.Threading.Tasks;

namespace GenericAPI.Services
{
    /// <summary>
    /// Interface for caching service operations
    /// </summary>
    public interface ICacheService
    {        /// <summary>
        /// Gets a cached value by key
        /// </summary>
        /// <typeparam name="T">Type of cached object</typeparam>
        /// <param name="key">Cache key</param>
        /// <returns>Cached value or null if not found</returns>
        Task<T?> GetAsync<T>(string key);

        /// <summary>
        /// Sets a value in cache with expiration
        /// </summary>
        /// <typeparam name="T">Type of object to cache</typeparam>
        /// <param name="key">Cache key</param>
        /// <param name="value">Value to cache</param>
        /// <param name="expiration">Cache expiration time</param>
        Task SetAsync<T>(string key, T value, TimeSpan? expiration = null);

        /// <summary>
        /// Removes a value from cache
        /// </summary>
        /// <param name="key">Cache key</param>
        Task RemoveAsync(string key);

        /// <summary>
        /// Removes all cache entries matching a pattern
        /// </summary>
        /// <param name="pattern">Pattern to match keys</param>
        Task RemoveByPatternAsync(string pattern);

        /// <summary>
        /// Checks if a key exists in cache
        /// </summary>
        /// <param name="key">Cache key</param>
        /// <returns>True if key exists</returns>
        Task<bool> ExistsAsync(string key);        /// <summary>
        /// Gets or sets a cached value with a factory function
        /// </summary>
        /// <typeparam name="T">Type of cached object</typeparam>
        /// <param name="key">Cache key</param>
        /// <param name="factory">Function to create value if not cached</param>
        /// <param name="expiration">Cache expiration time</param>
        /// <returns>Cached or newly created value</returns>
        Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null);
    }
}
