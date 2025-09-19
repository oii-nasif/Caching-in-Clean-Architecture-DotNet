using Microsoft.Extensions.Caching.Memory;
using System.Collections.Concurrent;
using System.Text.Json;

namespace Infrastructure.Cache;

public class InMemoryCacheProvider : ICacheProvider
{
    private readonly IMemoryCache _memoryCache;
    private readonly ConcurrentDictionary<string, byte> _cacheKeys;

    public InMemoryCacheProvider(IMemoryCache memoryCache)
    {
        _memoryCache = memoryCache;
        _cacheKeys = new ConcurrentDictionary<string, byte>();
    }

    public Task<T?> GetAsync<T>(string key) where T : class
    {
        if (_memoryCache.TryGetValue(key, out var value))
        {
            if (value is string jsonString)
            {
                return Task.FromResult(JsonSerializer.Deserialize<T>(jsonString));
            }
            return Task.FromResult(value as T);
        }
        return Task.FromResult<T?>(null);
    }

    public Task SetAsync<T>(string key, T value, TimeSpan? expiry = null) where T : class
    {
        var options = new MemoryCacheEntryOptions();

        if (expiry.HasValue)
        {
            options.SetAbsoluteExpiration(expiry.Value);
        }
        else
        {
            options.SetSlidingExpiration(TimeSpan.FromMinutes(30));
        }

        options.RegisterPostEvictionCallback((evictedKey, evictedValue, reason, state) =>
        {
            _cacheKeys.TryRemove(evictedKey.ToString()!, out _);
        });

        var serialized = JsonSerializer.Serialize(value);
        _memoryCache.Set(key, serialized, options);
        _cacheKeys.TryAdd(key, 1);

        return Task.CompletedTask;
    }

    public Task RemoveAsync(string key)
    {
        _memoryCache.Remove(key);
        _cacheKeys.TryRemove(key, out _);
        return Task.CompletedTask;
    }

    public Task<bool> ExistsAsync(string key)
    {
        return Task.FromResult(_memoryCache.TryGetValue(key, out _));
    }

    public async Task<IDictionary<string, T>> GetMultipleAsync<T>(IEnumerable<string> keys) where T : class
    {
        var result = new Dictionary<string, T>();

        foreach (var key in keys)
        {
            var value = await GetAsync<T>(key);
            if (value != null)
            {
                result.Add(key, value);
            }
        }

        return result;
    }

    public Task RemoveByPatternAsync(string pattern)
    {
        var keysToRemove = _cacheKeys.Keys.Where(k => MatchesPattern(k, pattern)).ToList();

        foreach (var key in keysToRemove)
        {
            _memoryCache.Remove(key);
            _cacheKeys.TryRemove(key, out _);
        }

        return Task.CompletedTask;
    }

    private bool MatchesPattern(string key, string pattern)
    {
        // Simple pattern matching (supports * wildcard)
        var regexPattern = "^" + pattern.Replace("*", ".*") + "$";
        return System.Text.RegularExpressions.Regex.IsMatch(key, regexPattern);
    }
}