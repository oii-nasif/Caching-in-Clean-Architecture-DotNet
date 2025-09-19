# Caching in Clean Architecture - Complete Documentation
## RiskMonitor Application Cache Implementation Guide

---

## Table of Contents
1. [Overview](#overview)
2. [Architecture Layers](#architecture-layers)
3. [Core Components](#core-components)
4. [Implementation Details](#implementation-details)
5. [Usage Patterns](#usage-patterns)
6. [Best Practices](#best-practices)
7. [Testing Strategy](#testing-strategy)
8. [Configuration](#configuration)
9. [Troubleshooting](#troubleshooting)

---

## 1. Overview

### Purpose
The caching mechanism in the RiskMonitor Clean Architecture implementation provides a high-performance, scalable solution for temporary data storage, reducing database load and improving application responsiveness.

### Key Features
- **Clean Architecture Compliance**: Cache abstraction in Application layer, implementation in Infrastructure layer
- **Dependency Injection**: Full DI support through interfaces
- **Type Safety**: Generic methods with strong typing
- **Expiration Support**: Configurable TTL (Time To Live)
- **Session-based Caching**: Support for user session-specific cache entries

### Current Use Case
Primary implementation focuses on Portfolio NAV (Net Asset Value) upload workflow, where large datasets are temporarily cached during multi-step upload and validation processes.

---

## 2. Architecture Layers

### Layer Separation

```
┌─────────────────────────────────────────────────┐
│                  Presentation Layer              │
│               (Controllers, Views)               │
└─────────────────────────────────────────────────┘
                         │
                         ▼
┌─────────────────────────────────────────────────┐
│                 Application Layer                │
│        (Commands, Queries, Interfaces)           │
│   • ICacheService (Contract)                     │
│   • CacheKeys (Key Generation)                   │
│   • CacheDataModels (DTOs)                       │
└─────────────────────────────────────────────────┘
                         │
                         ▼
┌─────────────────────────────────────────────────┐
│               Infrastructure Layer               │
│            (External Dependencies)               │
│   • CacheService (Implementation)                │
│   • Redis/In-Memory Provider                     │
└─────────────────────────────────────────────────┘
                         │
                         ▼
┌─────────────────────────────────────────────────┐
│                  Domain Layer                    │
│              (Entities, Value Objects)           │
└─────────────────────────────────────────────────┘
```

### Dependency Flow
- **Application Layer** → Defines contracts (ICacheService)
- **Infrastructure Layer** → Implements contracts
- **Domain Layer** → Remains independent
- **Presentation Layer** → Uses Application layer services

---

## 3. Core Components

### 3.1 ICacheService Interface
**Location**: `Application/Contracts/Infrastructure/ICacheService.cs`

```csharp
public interface ICacheService
{
    Task<T> GetAsync<T>(string key) where T : class;
    Task SetAsync<T>(string key, T value, TimeSpan? expiry = null) where T : class;
    Task RemoveAsync(string key);
    Task<bool> ExistsAsync(string key);
}
```

**Purpose**: Defines the contract for cache operations, ensuring loose coupling between layers.

### 3.2 CacheService Implementation
**Location**: `Infrastructure/Cache/CacheService.cs`

```csharp
public class CacheService : ICacheService
{
    private readonly ICacheManager _cacheManager;
    private readonly ICache _cache;

    public CacheService(ICacheManager cacheManager)
    {
        _cacheManager = cacheManager;
        _cache = _cacheManager.GetCache("CleanArchCache");
    }
    // Implementation methods...
}
```

**Key Features**:
- Uses underlying cache manager (DotNet.Core.Utility.Caching)
- Named cache instance ("CleanArchCache")
- Thread-safe operations
- Null-safe returns

### 3.3 CacheKeys Static Class
**Location**: `Application/Common/CacheKeys.cs`

```csharp
public static class CacheKeys
{
    public static string NavUploadData(string sessionId)
        => $"nav_upload:{sessionId}:data";

    public static string NavUploadDataSource(string sessionId)
        => $"nav_upload:{sessionId}:datasource";
}
```

**Benefits**:
- Centralized key management
- Type-safe key generation
- Consistent naming convention
- Prevents key collisions

### 3.4 Cache Data Models
**Location**: `Application/Common/CacheDataModels.cs`

```csharp
public class NavUploadDataSource
{
    public int DataSourceId { get; set; }
}
```

**Purpose**: DTOs specifically designed for cache storage, keeping domain entities clean.

---

## 4. Implementation Details

### 4.1 Dependency Injection Setup
**Location**: `Infrastructure/InfrastructureServiceRegistration.cs`

```csharp
public static class InfrastructureServiceRegistration
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Cache services registration
        services.AddSingleton<ICacheService, CacheService>();

        // Other service registrations...
        return services;
    }
}
```

**Registration Type**: Singleton
- Single instance throughout application lifetime
- Shared across all requests
- Thread-safe implementation required

### 4.2 Real-World Implementation Example

#### Upload Handler
**Location**: `Application/Features/Portfolio/PortfolioNav/Command/UploadPortfolioNav`

```csharp
public class UploadPortfolioNavCommandHandler
{
    private readonly ICacheService _cacheService;
    private readonly TimeSpan _cacheExpiry = TimeSpan.FromMinutes(30);

    public async Task<Response> Handle(Command request, CancellationToken token)
    {
        // Validate data...

        // Cache validated data for later processing
        if (!string.IsNullOrEmpty(request.SessionId))
        {
            await _cacheService.SetAsync(
                CacheKeys.NavUploadData(request.SessionId),
                data,
                _cacheExpiry
            );

            await _cacheService.SetAsync(
                CacheKeys.NavUploadDataSource(request.SessionId),
                new NavUploadDataSource { DataSourceId = request.DataSourceId },
                _cacheExpiry
            );
        }
    }
}
```

#### Save Handler
**Location**: `Application/Features/Portfolio/PortfolioNav/Command/SaveUploadedNav`

```csharp
public class SaveUploadedNavCommandHandler
{
    public async Task<Response> Handle(Command request, CancellationToken token)
    {
        // Retrieve data from cache
        var cachedData = await _cacheService.GetAsync<List<NavUploadDto>>(
            CacheKeys.NavUploadData(request.SessionId)
        );

        var cachedDataSource = await _cacheService.GetAsync<NavUploadDataSource>(
            CacheKeys.NavUploadDataSource(request.SessionId)
        );

        if (cachedData == null || !cachedData.Any())
        {
            return new Response
            {
                Success = false,
                Message = "No cached data found. Upload session may have expired."
            };
        }

        // Process cached data...
    }
}
```

---

## 5. Usage Patterns

### 5.1 Multi-Step Process Pattern
Perfect for workflows requiring temporary state management:

```
Step 1: Upload & Validate
    ↓ (Cache data)
Step 2: User Review
    ↓ (Retrieve from cache)
Step 3: Confirm & Save
    ↓ (Clear cache)
Complete
```

### 5.2 Session-Based Caching
- Each user session has unique cache entries
- Prevents data conflicts between concurrent users
- Automatic cleanup via TTL

### 5.3 Common Operations

#### Setting Cache with Expiry
```csharp
await _cacheService.SetAsync(
    key: "user:123:preferences",
    value: userPreferences,
    expiry: TimeSpan.FromHours(1)
);
```

#### Retrieving with Null Check
```csharp
var data = await _cacheService.GetAsync<UserData>("user:123:data");
if (data == null)
{
    // Handle cache miss
    data = await LoadFromDatabase();
    await _cacheService.SetAsync("user:123:data", data);
}
```

#### Conditional Removal
```csharp
if (await _cacheService.ExistsAsync(cacheKey))
{
    await _cacheService.RemoveAsync(cacheKey);
}
```

---

## 6. Best Practices

### 6.1 Key Naming Conventions
```
Pattern: {feature}:{identifier}:{datatype}
Examples:
- nav_upload:session123:data
- user:456:preferences
- report:789:generated
```

### 6.2 Expiration Strategy
- **Short-lived data**: 5-30 minutes (upload sessions)
- **User sessions**: 1-4 hours
- **Reference data**: 24 hours
- **Static content**: 7-30 days

### 6.3 Error Handling
```csharp
try
{
    var data = await _cacheService.GetAsync<T>(key);
    if (data == null)
    {
        // Cache miss - normal flow
        return await LoadFromPrimarySource();
    }
    return data;
}
catch (Exception ex)
{
    _logger.LogWarning(ex, "Cache operation failed, falling back to database");
    return await LoadFromPrimarySource();
}
```

### 6.4 Data Serialization
- Use simple DTOs for cache storage
- Avoid circular references
- Keep cached objects lightweight
- Consider compression for large datasets

---

## 7. Testing Strategy

### 7.1 Unit Testing
```csharp
[Test]
public async Task Should_Store_And_Retrieve_Data()
{
    // Arrange
    var mockCacheService = new Mock<ICacheService>();
    var testData = new TestDto { Id = 1, Name = "Test" };

    mockCacheService
        .Setup(x => x.GetAsync<TestDto>(It.IsAny<string>()))
        .ReturnsAsync(testData);

    // Act
    var result = await service.GetData("key");

    // Assert
    Assert.NotNull(result);
    Assert.AreEqual(testData.Id, result.Id);
}
```

### 7.2 Integration Testing
**Location**: `Application.UnitTests/Features/Portfolio/PortfolioNav/Commands/CacheIntegrationTests.cs`

Key test scenarios:
- Cache hit/miss scenarios
- Expiration behavior
- Concurrent access
- Large data handling
- Session isolation

---

## 8. Configuration

### 8.1 Application Settings
```json
{
  "CacheSettings": {
    "DefaultExpiration": "00:30:00",
    "Provider": "InMemory",
    "ConnectionString": "localhost:6379",
    "InstanceName": "CleanArchCache"
  }
}
```

### 8.2 Environment-Specific Configuration
- **Development**: In-memory cache
- **Staging**: Redis with shorter TTL
- **Production**: Redis cluster with optimal TTL

---

## 9. Troubleshooting

### 9.1 Common Issues

| Issue | Cause | Solution |
|-------|-------|----------|
| Cache miss on valid key | Expired entry | Check TTL settings |
| Memory growth | No expiration | Set appropriate TTL |
| Concurrent access issues | Race conditions | Implement cache-aside pattern |
| Serialization errors | Complex objects | Use simple DTOs |
| Key collisions | Poor naming | Use CacheKeys class |

### 9.2 Performance Monitoring
```csharp
public async Task<T> GetWithMetrics<T>(string key) where T : class
{
    var stopwatch = Stopwatch.StartNew();
    try
    {
        var result = await _cacheService.GetAsync<T>(key);
        _logger.LogDebug($"Cache {(result != null ? "hit" : "miss")} for {key} in {stopwatch.ElapsedMilliseconds}ms");
        return result;
    }
    finally
    {
        stopwatch.Stop();
    }
}
```

### 9.3 Cache Health Check
```csharp
public class CacheHealthCheck : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _cacheService.SetAsync("health_check", "OK", TimeSpan.FromSeconds(1));
            var result = await _cacheService.GetAsync<string>("health_check");

            return result == "OK"
                ? HealthCheckResult.Healthy("Cache is operational")
                : HealthCheckResult.Unhealthy("Cache read/write failed");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy($"Cache check failed: {ex.Message}");
        }
    }
}
```

---

## Summary

The caching implementation in RiskMonitor's Clean Architecture provides:

✅ **Clean separation of concerns** - Interface in Application, implementation in Infrastructure
✅ **Testability** - Easy to mock ICacheService for unit tests
✅ **Flexibility** - Can swap cache providers without changing business logic
✅ **Type safety** - Generic methods ensure compile-time checking
✅ **Session support** - Built-in multi-user isolation
✅ **Performance** - Reduces database load for temporary data

This architecture ensures maintainable, scalable, and testable caching solution that adheres to Clean Architecture principles while providing practical benefits for real-world applications.

---

*Document Version: 1.0*
*Last Updated: 2025*
*Architecture: Clean Architecture with MediatR Pattern*