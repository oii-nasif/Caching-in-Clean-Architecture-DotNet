namespace Application.Contracts.Infrastructure;

public interface ICacheService
{
    Task<T?> GetAsync<T>(string key) where T : class;
    Task SetAsync<T>(string key, T value, TimeSpan? expiry = null) where T : class;
    Task RemoveAsync(string key);
    Task<bool> ExistsAsync(string key);
    Task<IDictionary<string, T>> GetMultipleAsync<T>(IEnumerable<string> keys) where T : class;
    Task RemoveByPatternAsync(string pattern);
}