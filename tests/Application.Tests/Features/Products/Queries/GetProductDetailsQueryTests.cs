using Application.Contracts.Infrastructure;
using Application.Features.Products.Queries;
using Application.ViewModels;
using Microsoft.Extensions.Logging;
using Moq;

namespace Application.Tests.Features.Products.Queries;

public class GetProductDetailsQueryTests
{
    private readonly Mock<ICacheService> _mockCacheService;
    private readonly Mock<ILogger<GetProductDetailsQueryHandler>> _mockLogger;
    private readonly GetProductDetailsQueryHandler _handler;

    public GetProductDetailsQueryTests()
    {
        _mockCacheService = new Mock<ICacheService>();
        _mockLogger = new Mock<ILogger<GetProductDetailsQueryHandler>>();
        _handler = new GetProductDetailsQueryHandler(_mockCacheService.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task Handle_Should_Return_From_Cache_When_Available()
    {
        // Arrange
        var productId = 123;
        var cachedProduct = new ProductDetailsVm
        {
            Id = productId,
            Name = "Cached Product",
            Price = 99.99m
        };

        _mockCacheService
            .Setup(x => x.GetAsync<ProductDetailsVm>(It.IsAny<string>()))
            .ReturnsAsync(cachedProduct);

        var query = new GetProductDetailsQuery(productId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.True(result.FromCache);
        Assert.Equal(cachedProduct.Id, result.Data?.Id);
        Assert.Equal("Retrieved from cache", result.Message);

        _mockCacheService.Verify(x => x.GetAsync<ProductDetailsVm>(It.IsAny<string>()), Times.Once);
        _mockCacheService.Verify(x => x.SetAsync(It.IsAny<string>(), It.IsAny<ProductDetailsVm>(), It.IsAny<TimeSpan?>()), Times.Never);
    }

    [Fact]
    public async Task Handle_Should_Fetch_And_Cache_When_Cache_Miss()
    {
        // Arrange
        var productId = 123;

        _mockCacheService
            .Setup(x => x.GetAsync<ProductDetailsVm>(It.IsAny<string>()))
            .ReturnsAsync((ProductDetailsVm?)null);

        _mockCacheService
            .Setup(x => x.SetAsync(It.IsAny<string>(), It.IsAny<ProductDetailsVm>(), It.IsAny<TimeSpan?>()))
            .Returns(Task.CompletedTask);

        var query = new GetProductDetailsQuery(productId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.False(result.FromCache);
        Assert.Equal(productId, result.Data?.Id);
        Assert.Equal("Retrieved from database and cached", result.Message);

        _mockCacheService.Verify(x => x.GetAsync<ProductDetailsVm>(It.IsAny<string>()), Times.Once);
        _mockCacheService.Verify(x => x.SetAsync(It.IsAny<string>(), It.IsAny<ProductDetailsVm>(), It.IsAny<TimeSpan?>()), Times.Once);
    }

    [Fact]
    public async Task Handle_Should_Generate_Correct_Product_Data()
    {
        // Arrange
        var productId = 456;

        _mockCacheService
            .Setup(x => x.GetAsync<ProductDetailsVm>(It.IsAny<string>()))
            .ReturnsAsync((ProductDetailsVm?)null);

        var query = new GetProductDetailsQuery(productId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result.Data);
        Assert.Equal(productId, result.Data.Id);
        Assert.Equal($"Product {productId}", result.Data.Name);
        Assert.Equal($"SKU-{productId:D4}", result.Data.SKU);
        Assert.Equal("Electronics", result.Data.Category);
        Assert.True(result.Data.IsActive);
    }
}