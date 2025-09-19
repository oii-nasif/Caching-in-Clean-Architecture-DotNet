using Application.Common;
using Application.Contracts.Infrastructure;
using Application.ViewModels;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Features.Products.Queries;

public class GetProductDetailsQuery : IRequest<CacheResponseVm<ProductDetailsVm>>
{
    public int ProductId { get; set; }

    public GetProductDetailsQuery(int productId)
    {
        ProductId = productId;
    }
}

public class GetProductDetailsQueryHandler : IRequestHandler<GetProductDetailsQuery, CacheResponseVm<ProductDetailsVm>>
{
    private readonly ICacheService _cacheService;
    private readonly ILogger<GetProductDetailsQueryHandler> _logger;
    private readonly TimeSpan _cacheExpiry = TimeSpan.FromMinutes(15);

    public GetProductDetailsQueryHandler(
        ICacheService cacheService,
        ILogger<GetProductDetailsQueryHandler> logger)
    {
        _cacheService = cacheService;
        _logger = logger;
    }

    public async Task<CacheResponseVm<ProductDetailsVm>> Handle(GetProductDetailsQuery request, CancellationToken cancellationToken)
    {
        var cacheKey = CacheKeys.ProductDetails(request.ProductId);

        // Try to get from cache first
        var cachedProduct = await _cacheService.GetAsync<ProductDetailsVm>(cacheKey);
        if (cachedProduct != null)
        {
            _logger.LogInformation("Product {ProductId} retrieved from cache", request.ProductId);

            return new CacheResponseVm<ProductDetailsVm>
            {
                Data = cachedProduct,
                FromCache = true,
                Success = true,
                Message = "Retrieved from cache"
            };
        }

        // Cache miss - simulate fetching from database
        _logger.LogInformation("Cache miss for product {ProductId}, fetching from database", request.ProductId);

        // Simulate database call
        await Task.Delay(100, cancellationToken); // Simulate DB latency

        var product = new ProductDetailsVm
        {
            Id = request.ProductId,
            Name = $"Product {request.ProductId}",
            Description = $"This is a detailed description for product {request.ProductId}",
            Price = 99.99m * request.ProductId,
            StockLevel = Random.Shared.Next(0, 200),
            SKU = $"SKU-{request.ProductId:D4}",
            Category = "Electronics",
            IsActive = true,
            CreatedAt = DateTime.UtcNow.AddDays(-30),
            UpdatedAt = DateTime.UtcNow.AddDays(-1)
        };

        // Store in cache for future requests
        await _cacheService.SetAsync(cacheKey, product, _cacheExpiry);

        _logger.LogInformation("Product {ProductId} cached with expiry {Expiry}", request.ProductId, _cacheExpiry);

        return new CacheResponseVm<ProductDetailsVm>
        {
            Data = product,
            FromCache = false,
            Success = true,
            Message = "Retrieved from database and cached"
        };
    }
}