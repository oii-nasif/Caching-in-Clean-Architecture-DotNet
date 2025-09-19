using Application.Contracts.Infrastructure;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Cache;

public class CacheService : ICacheService
{
    private readonly ICacheProvider _cacheProvider;
    private readonly ILogger<CacheService> _logger;

    public CacheService(ICacheProvider cacheProvider, ILogger<CacheService> logger)
    {
        _cacheProvider = cacheProvider;
        _logger = logger;
    }

    public async Task<T?> GetAsync<T>(string key) where T : class
    {
        try
        {
            _logger.LogDebug("Retrieving cache key: {Key}", key);
            var result = await _cacheProvider.GetAsync<T>(key);

            if (result != null)
            {
                _logger.LogDebug("Cache hit for key: {Key}", key);
            }
            else
            {
                _logger.LogDebug("Cache miss for key: {Key}", key);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving cache key: {Key}", key);
            return null;
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null) where T : class
    {
        try
        {
            _logger.LogDebug("Setting cache key: {Key} with expiry: {Expiry}", key, expiry);
            await _cacheProvider.SetAsync(key, value, expiry ?? TimeSpan.FromMinutes(30));
            _logger.LogDebug("Successfully set cache key: {Key}", key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting cache key: {Key}", key);
            throw;
        }
    }

    public async Task RemoveAsync(string key)
    {
        try
        {
            _logger.LogDebug("Removing cache key: {Key}", key);
            await _cacheProvider.RemoveAsync(key);
            _logger.LogDebug("Successfully removed cache key: {Key}", key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing cache key: {Key}", key);
            throw;
        }
    }

    public async Task<bool> ExistsAsync(string key)
    {
        try
        {
            var exists = await _cacheProvider.ExistsAsync(key);
            _logger.LogDebug("Cache key {Key} exists: {Exists}", key, exists);
            return exists;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if cache key exists: {Key}", key);
            return false;
        }
    }

    public async Task<IDictionary<string, T>> GetMultipleAsync<T>(IEnumerable<string> keys) where T : class
    {
        try
        {
            var keysList = keys.ToList();
            _logger.LogDebug("Retrieving multiple cache keys. Count: {Count}", keysList.Count);

            var result = await _cacheProvider.GetMultipleAsync<T>(keysList);

            _logger.LogDebug("Retrieved {Count} of {Total} cache keys", result.Count, keysList.Count);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving multiple cache keys");
            return new Dictionary<string, T>();
        }
    }

    public async Task RemoveByPatternAsync(string pattern)
    {
        try
        {
            _logger.LogDebug("Removing cache keys matching pattern: {Pattern}", pattern);
            await _cacheProvider.RemoveByPatternAsync(pattern);
            _logger.LogDebug("Successfully removed cache keys matching pattern: {Pattern}", pattern);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing cache keys by pattern: {Pattern}", pattern);
            throw;
        }
    }
}