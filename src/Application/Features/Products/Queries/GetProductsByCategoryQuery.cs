using Application.Common;
using Application.Contracts.Infrastructure;
using Application.ViewModels;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Features.Products.Queries;

public class GetProductsByCategoryQuery : IRequest<List<ProductVm>>
{
    public string Category { get; set; }

    public GetProductsByCategoryQuery(string category)
    {
        Category = category;
    }
}

public class GetProductsByCategoryQueryHandler : IRequestHandler<GetProductsByCategoryQuery, List<ProductVm>>
{
    private readonly ICacheService _cacheService;
    private readonly ILogger<GetProductsByCategoryQueryHandler> _logger;
    private readonly TimeSpan _cacheExpiry = TimeSpan.FromMinutes(10);

    public GetProductsByCategoryQueryHandler(
        ICacheService cacheService,
        ILogger<GetProductsByCategoryQueryHandler> logger)
    {
        _cacheService = cacheService;
        _logger = logger;
    }

    public async Task<List<ProductVm>> Handle(GetProductsByCategoryQuery request, CancellationToken cancellationToken)
    {
        var cacheKey = CacheKeys.ProductsByCategory(request.Category);

        // Try to get from cache first
        var cachedProducts = await _cacheService.GetAsync<List<ProductVm>>(cacheKey);
        if (cachedProducts != null)
        {
            _logger.LogInformation("Products for category {Category} retrieved from cache. Count: {Count}",
                request.Category, cachedProducts.Count);
            return cachedProducts;
        }

        // Cache miss - simulate fetching from database
        _logger.LogInformation("Cache miss for category {Category}, fetching from database", request.Category);

        // Simulate database call
        await Task.Delay(200, cancellationToken); // Simulate DB latency

        var products = GenerateProductsForCategory(request.Category);

        // Store in cache for future requests
        await _cacheService.SetAsync(cacheKey, products, _cacheExpiry);

        _logger.LogInformation("Products for category {Category} cached. Count: {Count}",
            request.Category, products.Count);

        return products;
    }

    private List<ProductVm> GenerateProductsForCategory(string category)
    {
        var products = new List<ProductVm>();
        var productCount = Random.Shared.Next(3, 8);

        for (int i = 1; i <= productCount; i++)
        {
            products.Add(new ProductVm
            {
                Id = Random.Shared.Next(1, 1000),
                Name = $"{category} Product {i}",
                Description = $"High-quality {category.ToLower()} product #{i}",
                Price = Math.Round((decimal)(Random.Shared.NextDouble() * 500 + 50), 2),
                StockLevel = Random.Shared.Next(0, 100),
                SKU = $"{category.ToUpper()}-{i:D3}",
                Category = category,
                IsActive = Random.Shared.NextDouble() > 0.1 // 90% active
            });
        }

        return products;
    }
}