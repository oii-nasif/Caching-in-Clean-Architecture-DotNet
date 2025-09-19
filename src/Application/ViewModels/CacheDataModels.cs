namespace Application.ViewModels;

// Generic cache wrapper for metadata
public class CachedData<T> where T : class
{
    public T Data { get; set; } = null!;
    public DateTime CachedAt { get; set; }
    public string? ETag { get; set; }
    public TimeSpan? TimeToLive { get; set; }
}

// Report cache model
public class ReportCacheVm
{
    public string ReportId { get; set; } = string.Empty;
    public byte[] Content { get; set; } = Array.Empty<byte>();
    public string Format { get; set; } = string.Empty;
    public DateTime GeneratedAt { get; set; }
    public string ReportType { get; set; } = string.Empty;
}

// Response wrapper for cached data
public class CacheResponseVm<T> where T : class
{
    public T? Data { get; set; }
    public bool FromCache { get; set; }
    public DateTime? CachedAt { get; set; }
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
}