using MirrorBot.Worker.Services.AI.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MirrorBot.Worker.Services.AI.Implementations
{
    public class MemoryResponseCache : IResponseCache
    {
        private class CacheEntry
        {
            public string Value { get; set; }
            public DateTime ExpirationTime { get; set; }
        }

        private readonly Dictionary<string, CacheEntry> _cache = new();
        private readonly object _lockObject = new object();
        private readonly TimeSpan _defaultExpiration = TimeSpan.FromHours(24);

        public Task<string> GetAsync(string key)
        {
            if (string.IsNullOrEmpty(key))
                return Task.FromResult<string>(null);

            lock (_lockObject)
            {
                if (_cache.TryGetValue(key, out var entry))
                {
                    // Проверяем не истекла ли запись
                    if (entry.ExpirationTime > DateTime.UtcNow)
                    {
                        return Task.FromResult(entry.Value);
                    }
                    else
                    {
                        // Удаляем истекшую запись
                        _cache.Remove(key);
                    }
                }
            }

            return Task.FromResult<string>(null);
        }

        public Task SetAsync(string key, string value, TimeSpan? expiration = null)
        {
            if (string.IsNullOrEmpty(key))
                return Task.CompletedTask;

            lock (_lockObject)
            {
                var expirationTime = DateTime.UtcNow.Add(expiration ?? _defaultExpiration);
                _cache[key] = new CacheEntry
                {
                    Value = value,
                    ExpirationTime = expirationTime
                };
            }

            return Task.CompletedTask;
        }

        public Task<bool> ExistsAsync(string key)
        {
            if (string.IsNullOrEmpty(key))
                return Task.FromResult(false);

            lock (_lockObject)
            {
                if (_cache.TryGetValue(key, out var entry))
                {
                    if (entry.ExpirationTime > DateTime.UtcNow)
                    {
                        return Task.FromResult(true);
                    }
                    else
                    {
                        _cache.Remove(key);
                    }
                }
            }

            return Task.FromResult(false);
        }

        public Task RemoveAsync(string key)
        {
            if (string.IsNullOrEmpty(key))
                return Task.CompletedTask;

            lock (_lockObject)
            {
                _cache.Remove(key);
            }

            return Task.CompletedTask;
        }

        public Task ClearAsync()
        {
            lock (_lockObject)
            {
                _cache.Clear();
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Очистка истекших записей (периодическая очистка)
        /// </summary>
        public void CleanupExpiredEntries()
        {
            lock (_lockObject)
            {
                var expiredKeys = _cache
                    .Where(x => x.Value.ExpirationTime <= DateTime.UtcNow)
                    .Select(x => x.Key)
                    .ToList();

                foreach (var key in expiredKeys)
                {
                    _cache.Remove(key);
                }
            }
        }
    }
}
