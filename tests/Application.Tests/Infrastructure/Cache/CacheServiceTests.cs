using Application.Contracts.Infrastructure;
using Infrastructure.Cache;
using Microsoft.Extensions.Logging;
using Moq;

namespace Application.Tests.Infrastructure.Cache;

public class CacheServiceTests
{
    private readonly Mock<ICacheProvider> _mockCacheProvider;
    private readonly Mock<ILogger<CacheService>> _mockLogger;
    private readonly CacheService _cacheService;

    public CacheServiceTests()
    {
        _mockCacheProvider = new Mock<ICacheProvider>();
        _mockLogger = new Mock<ILogger<CacheService>>();
        _cacheService = new CacheService(_mockCacheProvider.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task GetAsync_Should_Return_Cached_Value_When_Available()
    {
        // Arrange
        var key = "test-key";
        var expectedValue = new TestObject { Name = "Test", Value = 123 };

        _mockCacheProvider
            .Setup(x => x.GetAsync<TestObject>(key))
            .ReturnsAsync(expectedValue);

        // Act
        var result = await _cacheService.GetAsync<TestObject>(key);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedValue.Name, result.Name);
        Assert.Equal(expectedValue.Value, result.Value);

        _mockCacheProvider.Verify(x => x.GetAsync<TestObject>(key), Times.Once);
    }

    [Fact]
    public async Task GetAsync_Should_Return_Null_When_Cache_Miss()
    {
        // Arrange
        var key = "missing-key";

        _mockCacheProvider
            .Setup(x => x.GetAsync<TestObject>(key))
            .ReturnsAsync((TestObject?)null);

        // Act
        var result = await _cacheService.GetAsync<TestObject>(key);

        // Assert
        Assert.Null(result);

        _mockCacheProvider.Verify(x => x.GetAsync<TestObject>(key), Times.Once);
    }

    [Fact]
    public async Task GetAsync_Should_Return_Null_And_Log_Error_When_Exception_Occurs()
    {
        // Arrange
        var key = "error-key";
        var expectedException = new Exception("Cache error");

        _mockCacheProvider
            .Setup(x => x.GetAsync<TestObject>(key))
            .ThrowsAsync(expectedException);

        // Act
        var result = await _cacheService.GetAsync<TestObject>(key);

        // Assert
        Assert.Null(result);

        _mockCacheProvider.Verify(x => x.GetAsync<TestObject>(key), Times.Once);
        VerifyLogError($"Error retrieving cache key: {key}");
    }

    [Fact]
    public async Task SetAsync_Should_Cache_Value_With_Default_Expiry()
    {
        // Arrange
        var key = "test-key";
        var value = new TestObject { Name = "Test", Value = 123 };

        _mockCacheProvider
            .Setup(x => x.SetAsync(key, value, It.IsAny<TimeSpan?>()))
            .Returns(Task.CompletedTask);

        // Act
        await _cacheService.SetAsync(key, value);

        // Assert
        _mockCacheProvider.Verify(x => x.SetAsync(key, value, TimeSpan.FromMinutes(30)), Times.Once);
    }

    [Fact]
    public async Task SetAsync_Should_Cache_Value_With_Custom_Expiry()
    {
        // Arrange
        var key = "test-key";
        var value = new TestObject { Name = "Test", Value = 123 };
        var expiry = TimeSpan.FromMinutes(10);

        _mockCacheProvider
            .Setup(x => x.SetAsync(key, value, expiry))
            .Returns(Task.CompletedTask);

        // Act
        await _cacheService.SetAsync(key, value, expiry);

        // Assert
        _mockCacheProvider.Verify(x => x.SetAsync(key, value, expiry), Times.Once);
    }

    [Fact]
    public async Task RemoveAsync_Should_Remove_Cache_Entry()
    {
        // Arrange
        var key = "test-key";

        _mockCacheProvider
            .Setup(x => x.RemoveAsync(key))
            .Returns(Task.CompletedTask);

        // Act
        await _cacheService.RemoveAsync(key);

        // Assert
        _mockCacheProvider.Verify(x => x.RemoveAsync(key), Times.Once);
    }

    [Fact]
    public async Task ExistsAsync_Should_Return_True_When_Key_Exists()
    {
        // Arrange
        var key = "existing-key";

        _mockCacheProvider
            .Setup(x => x.ExistsAsync(key))
            .ReturnsAsync(true);

        // Act
        var result = await _cacheService.ExistsAsync(key);

        // Assert
        Assert.True(result);

        _mockCacheProvider.Verify(x => x.ExistsAsync(key), Times.Once);
    }

    [Fact]
    public async Task ExistsAsync_Should_Return_False_When_Key_Does_Not_Exist()
    {
        // Arrange
        var key = "missing-key";

        _mockCacheProvider
            .Setup(x => x.ExistsAsync(key))
            .ReturnsAsync(false);

        // Act
        var result = await _cacheService.ExistsAsync(key);

        // Assert
        Assert.False(result);

        _mockCacheProvider.Verify(x => x.ExistsAsync(key), Times.Once);
    }

    private void VerifyLogError(string message)
    {
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(message)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    private class TestObject
    {
        public string Name { get; set; } = string.Empty;
        public int Value { get; set; }
    }
}