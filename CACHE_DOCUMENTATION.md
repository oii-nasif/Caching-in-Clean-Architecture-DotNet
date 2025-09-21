## Caching in Clean Architecture - Complete Documentation
### Application Cache Implementation Guide

---

## Table of Contents
1. [Overview](#1-overview)
2. [Architecture Layers](#2-architecture-layers)
3. [Core Components](#3-core-components)
4. [Implementation Details](#4-implementation-details)
5. [Usage Patterns](#5-usage-patterns)
6. [Best Practices](#6-best-practices)
7. [Testing Strategy](#7-testing-strategy)
8. [Configuration](#8-configuration)
9. [Troubleshooting](#9-troubleshooting)

---

## 1. Overview

### Purpose
The caching mechanism in the Clean Architecture implementation provides a high-performance, scalable solution for temporary data storage, reducing database load and improving application responsiveness.

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
│                  Presentation Layer             │
│               (Controllers, Views)              │
└─────────────────────────────────────────────────┘
                         │
                         ▼
┌─────────────────────────────────────────────────┐
│                 Application Layer               │
│        (Commands, Queries, Interfaces)          │
│   • ICacheService (Contract)                    │
│   • CacheKeys (Key Generation)                  │
│   • CacheDataModels (DTOs)                      │
└─────────────────────────────────────────────────┘
                         │
                         ▼
┌─────────────────────────────────────────────────┐
│               Infrastructure Layer              │
│            (External Dependencies)              │
│   • CacheService (Implementation)               │
│   • Redis/In-Memory Provider                    │
└─────────────────────────────────────────────────┘
                         │
                         ▼
┌─────────────────────────────────────────────────┐
│                  Domain Layer                   │
│              (Entities, Value Objects)          │
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
    Task<IDictionary<string, T>> GetMultipleAsync<T>(IEnumerable<string> keys) where T : class;
    Task RemoveByPatternAsync(string pattern);
}
```

**Purpose**: Defines the contract for cache operations, ensuring loose coupling between layers.

### 3.2 CacheService Implementation
**Location**: `Infrastructure/Cache/CacheService.cs`

```csharp
public class CacheService : ICacheService
{
    private readonly ICacheProvider _cacheProvider;
    private readonly ILogger<CacheService> _logger;

    public CacheService(ICacheProvider cacheProvider, ILogger<CacheService> logger)
    {
        _cacheProvider = cacheProvider;
        _logger = logger;
    }

    public async Task<T> GetAsync<T>(string key) where T : class
    {
        try
        {
            return await _cacheProvider.GetAsync<T>(key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving cache key: {Key}", key);
            return null;
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null) where T : class
    {
        await _cacheProvider.SetAsync(key, value, expiry ?? TimeSpan.FromMinutes(30));
    }

    // Additional implementations...
}
```

**Key Features**:
- Provider abstraction for flexibility
- Built-in error handling
- Logging integration
- Thread-safe operations

### 3.3 CacheKeys Static Class
**Location**: `Application/Common/CacheKeys.cs`

```csharp
public static class CacheKeys
{
    // User-related keys
    public static string UserProfile(int userId)
        => $"user:{userId}:profile";

    public static string UserSession(string sessionId)
        => $"session:{sessionId}:data";

    // Product-related keys
    public static string ProductDetails(int productId)
        => $"product:{productId}:details";

    public static string ProductInventory(int productId)
        => $"product:{productId}:inventory";

    // Order-related keys
    public static string OrderSummary(int orderId)
        => $"order:{orderId}:summary";

    public static string CartItems(string cartId)
        => $"cart:{cartId}:items";

    // Report keys
    public static string DailyReport(DateTime date)
        => $"report:daily:{date:yyyy-MM-dd}";
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
// Generic cache wrapper for metadata
public class CachedData<T> where T : class
{
    public T Data { get; set; }
    public DateTime CachedAt { get; set; }
    public string ETag { get; set; }
}

// Example DTOs for caching
public class UserSessionData
{
    public string UserId { get; set; }
    public string Username { get; set; }
    public List<string> Permissions { get; set; }
    public DateTime LoginTime { get; set; }
}

public class ProductCacheDto
{
    public int Id { get; set; }
    public string Name { get; set; }
    public decimal Price { get; set; }
    public int StockLevel { get; set; }
}

public class ReportCacheData
{
    public string ReportId { get; set; }
    public byte[] Content { get; set; }
    public string Format { get; set; }
    public DateTime GeneratedAt { get; set; }
}

```
**Purpose**: DTOs specifically designed for cache storage, keeping domain entities clean.

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

### 4.2 Real-World Implementation Examples

#### Example 1: Product Service with Caching
**Location**: `Application/Features/Products/Queries/GetProductDetails`

```csharp
public class GetProductDetailsQueryHandler : IRequestHandler<GetProductDetailsQuery, ProductDto>
{
    private readonly IProductRepository _productRepository;
    private readonly ICacheService _cacheService;
    private readonly IMapper _mapper;
    private readonly TimeSpan _cacheExpiry = TimeSpan.FromMinutes(15);

    public GetProductDetailsQueryHandler(
        IProductRepository productRepository,
        ICacheService cacheService,
        IMapper mapper)
    {
        _productRepository = productRepository;
        _cacheService = cacheService;
        _mapper = mapper;
    }

    public async Task<ProductDto> Handle(GetProductDetailsQuery request, CancellationToken cancellationToken)
    {
        var cacheKey = CacheKeys.ProductDetails(request.ProductId);

        // Try to get from cache first
        var cachedProduct = await _cacheService.GetAsync<ProductDto>(cacheKey);
        if (cachedProduct != null)
        {
            return cachedProduct;
        }

        // Cache miss - fetch from database
        var product = await _productRepository.GetByIdAsync(request.ProductId);
        if (product == null)
        {
            throw new NotFoundException(nameof(Product), request.ProductId);
        }

        var productDto = _mapper.Map<ProductDto>(product);

        // Store in cache for future requests
        await _cacheService.SetAsync(cacheKey, productDto, _cacheExpiry);

        return productDto;
    }
}
```

#### Example 2: Shopping Cart Command Handler
**Location**: `Application/Features/Cart/Commands/AddToCart`

```csharp
public class AddToCartCommandHandler : IRequestHandler<AddToCartCommand, AddToCartResponse>
{
    private readonly ICacheService _cacheService;
    private readonly IProductRepository _productRepository;
    private readonly TimeSpan _cartExpiry = TimeSpan.FromHours(24);

    public async Task<AddToCartResponse> Handle(AddToCartCommand request, CancellationToken cancellationToken)
    {
        var cartKey = CacheKeys.CartItems(request.CartId);

        // Get existing cart items
        var cartItems = await _cacheService.GetAsync<List<CartItemDto>>(cartKey)
                        ?? new List<CartItemDto>();

        // Validate product exists
        var product = await _productRepository.GetByIdAsync(request.ProductId);
        if (product == null)
        {
            throw new NotFoundException(nameof(Product), request.ProductId);
        }

        // Add or update cart item
        var existingItem = cartItems.FirstOrDefault(x => x.ProductId == request.ProductId);
        if (existingItem != null)
        {
            existingItem.Quantity += request.Quantity;
        }
        else
        {
            cartItems.Add(new CartItemDto
            {
                ProductId = request.ProductId,
                ProductName = product.Name,
                Price = product.Price,
                Quantity = request.Quantity
            });
        }

        // Save updated cart to cache
        await _cacheService.SetAsync(cartKey, cartItems, _cartExpiry);

        return new AddToCartResponse
        {
            Success = true,
            CartItemCount = cartItems.Sum(x => x.Quantity)
        };
    }
}
```

#### Example 3: Report Generation with Caching
**Location**: `Application/Features/Reports/Commands/GenerateReport`

```csharp
public class GenerateReportCommandHandler : IRequestHandler<GenerateReportCommand, ReportResponse>
{
    private readonly IReportService _reportService;
    private readonly ICacheService _cacheService;
    private readonly TimeSpan _reportCacheExpiry = TimeSpan.FromHours(6);

    public async Task<ReportResponse> Handle(GenerateReportCommand request, CancellationToken cancellationToken)
    {
        var cacheKey = $"report:{request.ReportType}:{request.StartDate:yyyyMMdd}:{request.EndDate:yyyyMMdd}";

        // Check if report exists in cache
        var cachedReport = await _cacheService.GetAsync<ReportCacheData>(cacheKey);
        if (cachedReport != null && cachedReport.GeneratedAt > DateTime.UtcNow.AddHours(-6))
        {
            return new ReportResponse
            {
                Success = true,
                ReportData = cachedReport.Content,
                FromCache = true
            };
        }

        // Generate new report
        var reportData = await _reportService.GenerateAsync(
            request.ReportType,
            request.StartDate,
            request.EndDate
        );

        // Cache the generated report
        var cacheData = new ReportCacheData
        {
            ReportId = Guid.NewGuid().ToString(),
            Content = reportData,
            Format = request.Format,
            GeneratedAt = DateTime.UtcNow
        };

        await _cacheService.SetAsync(cacheKey, cacheData, _reportCacheExpiry);

        return new ReportResponse
        {
            Success = true,
            ReportData = reportData,
            FromCache = false
        };
    }
}

```

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
// Simple object caching
var userProfile = new UserProfile { Id = 123, Name = "John Doe", Email = "john@example.com" };
await _cacheService.SetAsync(
    key: CacheKeys.UserProfile(123),
    value: userProfile,
    expiry: TimeSpan.FromHours(1)
);

// Caching collection
var products = await _productRepository.GetAllAsync();
await _cacheService.SetAsync(
    key: "products:all",
    value: products,
    expiry: TimeSpan.FromMinutes(30)
);
```

#### Cache-Aside Pattern Implementation
```csharp
public async Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiry = null) where T : class
{
    // Try to get from cache
    var cached = await _cacheService.GetAsync<T>(key);
    if (cached != null)
    {
        return cached;
    }

    // Execute factory method to get data
    var data = await factory();

    // Store in cache
    if (data != null)
    {
        await _cacheService.SetAsync(key, data, expiry ?? TimeSpan.FromMinutes(10));
    }

    return data;
}

// Usage
var product = await GetOrSetAsync(
    CacheKeys.ProductDetails(productId),
    async () => await _productRepository.GetByIdAsync(productId),
    TimeSpan.FromMinutes(15)
);
```

#### Batch Operations
```csharp
// Getting multiple items
var keys = productIds.Select(id => CacheKeys.ProductDetails(id));
var products = await _cacheService.GetMultipleAsync<ProductDto>(keys);

// Removing by pattern
await _cacheService.RemoveByPatternAsync("user:123:*"); // Remove all user 123's cached data
```

#### Conditional Caching
```csharp
public async Task<OrderDto> GetOrderAsync(int orderId, bool bypassCache = false)
{
    var cacheKey = CacheKeys.OrderSummary(orderId);

    if (!bypassCache)
    {
        var cached = await _cacheService.GetAsync<OrderDto>(cacheKey);
        if (cached != null)
        {
            return cached;
        }
    }

    var order = await _orderRepository.GetByIdAsync(orderId);
    var orderDto = _mapper.Map<OrderDto>(order);

    // Only cache if order is completed
    if (order.Status == OrderStatus.Completed)
    {
        await _cacheService.SetAsync(cacheKey, orderDto, TimeSpan.FromDays(7));
    }

    return orderDto;
}

```

## 6. Best Practices

### 6.1 Key Naming Conventions
```
Pattern: {feature}:{identifier}:{datatype}
Examples:
- user:456:profile
- product:789:details
- cart:abc123:items
- session:xyz456:data
- report:daily:2024-01-15
- order:1234:summary
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

### 7.1 Unit Testing Examples
```csharp
[TestFixture]
public class ProductServiceTests
{
    private Mock<ICacheService> _mockCacheService;
    private Mock<IProductRepository> _mockProductRepository;
    private ProductService _service;

    [SetUp]
    public void Setup()
    {
        _mockCacheService = new Mock<ICacheService>();
        _mockProductRepository = new Mock<IProductRepository>();
        _service = new ProductService(_mockCacheService.Object, _mockProductRepository.Object);
    }

    [Test]
    public async Task GetProduct_Should_Return_From_Cache_When_Available()
    {
        // Arrange
        var productId = 123;
        var cachedProduct = new ProductDto { Id = productId, Name = "Cached Product" };

        _mockCacheService
            .Setup(x => x.GetAsync<ProductDto>(It.IsAny<string>()))
            .ReturnsAsync(cachedProduct);

        // Act
        var result = await _service.GetProductAsync(productId);

        // Assert
        Assert.AreEqual(cachedProduct.Id, result.Id);
        _mockProductRepository.Verify(x => x.GetByIdAsync(It.IsAny<int>()), Times.Never);
    }

    [Test]
    public async Task GetProduct_Should_Fetch_From_Database_When_Cache_Miss()
    {
        // Arrange
        var productId = 123;
        var dbProduct = new Product { Id = productId, Name = "DB Product" };

        _mockCacheService
            .Setup(x => x.GetAsync<ProductDto>(It.IsAny<string>()))
            .ReturnsAsync((ProductDto)null);

        _mockProductRepository
            .Setup(x => x.GetByIdAsync(productId))
            .ReturnsAsync(dbProduct);

        // Act
        var result = await _service.GetProductAsync(productId);

        // Assert
        Assert.AreEqual(dbProduct.Id, result.Id);
        _mockCacheService.Verify(x => x.SetAsync(It.IsAny<string>(), It.IsAny<ProductDto>(), It.IsAny<TimeSpan?>()), Times.Once);
    }
}
```

### 7.2 Integration Testing
```csharp
[TestFixture]
public class CacheIntegrationTests
{
    private ICacheService _cacheService;
    private IServiceProvider _serviceProvider;

    [OneTimeSetUp]
    public void OneTimeSetup()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ICacheService, InMemoryCacheService>();
        _serviceProvider = services.BuildServiceProvider();
        _cacheService = _serviceProvider.GetRequiredService<ICacheService>();
    }

    [Test]
    public async Task Cache_Should_Store_And_Retrieve_Complex_Objects()
    {
        // Arrange
        var key = "test:complex:object";
        var testData = new ComplexDto
        {
            Id = Guid.NewGuid(),
            Items = new List<ItemDto>
            {
                new ItemDto { Name = "Item1", Value = 100 },
                new ItemDto { Name = "Item2", Value = 200 }
            }
        };

        // Act
        await _cacheService.SetAsync(key, testData, TimeSpan.FromMinutes(5));
        var retrieved = await _cacheService.GetAsync<ComplexDto>(key);

        // Assert
        Assert.IsNotNull(retrieved);
        Assert.AreEqual(testData.Id, retrieved.Id);
        Assert.AreEqual(testData.Items.Count, retrieved.Items.Count);
    }

    [Test]
    public async Task Cache_Should_Expire_After_TTL()
    {
        // Arrange
        var key = "test:expiry";
        var value = "test value";

        // Act
        await _cacheService.SetAsync(key, value, TimeSpan.FromMilliseconds(100));
        await Task.Delay(150);
        var retrieved = await _cacheService.GetAsync<string>(key);

        // Assert
        Assert.IsNull(retrieved);
    }

    [Test]
    public async Task Cache_Should_Handle_Concurrent_Access()
    {
        // Arrange
        var key = "test:concurrent";
        var tasks = new List<Task>();

        // Act
        for (int i = 0; i < 100; i++)
        {
            var index = i;
            tasks.Add(Task.Run(async () =>
            {
                await _cacheService.SetAsync($"{key}:{index}", index, TimeSpan.FromMinutes(1));
                var result = await _cacheService.GetAsync<int>($"{key}:{index}");
                Assert.AreEqual(index, result);
            }));
        }

        await Task.WhenAll(tasks);

        // Assert - no exceptions should be thrown
        Assert.Pass();
    }
}
```

### 7.3 Performance Testing
```csharp
[Test]
public async Task Cache_Performance_Should_Be_Acceptable()
{
    var stopwatch = new Stopwatch();
    var iterations = 1000;

    // Write performance
    stopwatch.Start();
    for (int i = 0; i < iterations; i++)
    {
        await _cacheService.SetAsync($"perf:test:{i}", new { Id = i, Data = "test" });
    }
    stopwatch.Stop();

    Assert.Less(stopwatch.ElapsedMilliseconds / iterations, 10, "Write operations should be under 10ms");

    // Read performance
    stopwatch.Restart();
    for (int i = 0; i < iterations; i++)
    {
        await _cacheService.GetAsync<object>($"perf:test:{i}");
    }
    stopwatch.Stop();

    Assert.Less(stopwatch.ElapsedMilliseconds / iterations, 5, "Read operations should be under 5ms");
}

```

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

The caching implementation in Clean Architecture provides:

✅ **Clean separation of concerns** - Interface in Application layer, implementation in Infrastructure layer

✅ **Testability** - Easy to mock ICacheService for unit tests

✅ **Flexibility** - Can swap cache providers (Redis, Memcached, In-Memory) without changing business logic

✅ **Type safety** - Generic methods ensure compile-time type checking

✅ **Session support** - Built-in multi-user isolation for concurrent operations

✅ **Performance** - Reduces database load and improves response times

### Key Takeaways

1. **Architecture Compliance**: Cache abstraction follows Clean Architecture principles
2. **Dependency Inversion**: Application layer defines contracts, Infrastructure provides implementation
3. **Single Responsibility**: Each component has a clear, focused purpose
4. **Open/Closed Principle**: Easy to extend with new cache providers
5. **Interface Segregation**: Clean, minimal interfaces for cache operations

This architecture ensures a maintainable, scalable, and testable caching solution that adheres to Clean Architecture principles while providing practical benefits for real-world enterprise applications.

---

*Document Version: 1.0*
*Last Updated: 2025*
*Architecture: Clean Architecture with MediatR Pattern*
