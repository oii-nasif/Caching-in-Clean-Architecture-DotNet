# Clean Architecture with Caching Implementation

A production-ready .NET 8 Web API project demonstrating Clean Architecture principles with comprehensive caching implementation.

## 𓇻 Architecture Overview

```
├── src/
│   ├── Domain/                # Core business entities
│   ├── Application/           # Business logic, interfaces, and MediatR handlers
│   ├── Infrastructure/        # Cache implementation and external services
│   └── API/                  # Web API presentation layer
└── tests/
    └── Application.Tests/     # Unit tests
```

## 🚀 Getting Started

### Prerequisites
- .NET 8.0 SDK or later
- Visual Studio 2022 or VS Code

### Running the Application

1. **Clone the repository**
   ```bash
   git clone https://github.com/yourusername/Caching-in-Clean-Architecture-DotNet.git
   cd Caching-in-Clean-Architecture-DotNet
   ```

2. **Build the solution**
   ```bash
   dotnet build
   ```

3. **Run tests**
   ```bash
   dotnet test
   ```

4. **Start the API**
   ```bash
   cd src/API
   dotnet run
   ```

5. **Access Swagger UI**
   Navigate to: `https://localhost:7003/swagger` (HTTPS) or `http://localhost:5214/swagger` (HTTP)

## 📋 Features

### Cache Implementation
- ✅ **In-Memory Caching**: High-performance memory cache provider
- ✅ **Cache-Aside Pattern**: Automatic cache population on miss
- ✅ **TTL Support**: Configurable expiration times
- ✅ **Pattern Matching**: Remove cache entries by pattern
- ✅ **Session-based Caching**: User-specific cache isolation

### Clean Architecture Layers

#### Domain Layer
- **Entities**: Product, Order, User
- **Base Entity**: Common properties for all entities
- **Value Objects**: OrderStatus enum

#### Application Layer
- **Interfaces**: `ICacheService`, Repository interfaces
- **ViewModels**: ProductVm, OrderVm, UserVm, CartVm
- **MediatR Handlers**:
  - `GetProductDetailsQuery`
  - `GetProductsByCategoryQuery`
- **Cache Keys**: Centralized key generation

#### Infrastructure Layer
- **Cache Service**: Implementation with logging
- **Cache Providers**: In-memory provider with pattern matching
- **Dependency Injection**: Service registration

#### API Layer
- **Controllers**: Products, Cart
- **Swagger**: Auto-generated API documentation
- **Dependency Injection**: Complete service configuration

## 🔌 API Endpoints

### Products
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/products/{id}` | Get product details (cached) |
| GET | `/api/products/category/{category}` | Get products by category |
| DELETE | `/api/products/cache/{id}` | Clear product cache |

### Cart
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/cart/{cartId}` | Get cart items |
| POST | `/api/cart/{cartId}/items` | Add item to cart |
| PUT | `/api/cart/{cartId}/items/{productId}` | Update cart item quantity |
| DELETE | `/api/cart/{cartId}/items/{productId}` | Remove item from cart |
| DELETE | `/api/cart/{cartId}` | Clear entire cart |

## 🧪 Testing the Cache

### Using Swagger UI

1. **Test Cache Hit/Miss**:
   - Call `GET /api/products/1`
   - Note response: `"fromCache": false`
   - Call same endpoint again
   - Note response: `"fromCache": true`

2. **Test Cart Caching**:
   ```json
   POST /api/cart/user123/items
   {
     "productId": 1,
     "productName": "Laptop",
     "price": 999.99,
     "quantity": 2
   }
   ```
   - Cart is cached for 24 hours
   - Subsequent GET requests retrieve from cache

### Example Response
```json
{
  "data": {
    "id": 1,
    "name": "Product 1",
    "price": 99.99,
    "stockLevel": 150,
    "sku": "SKU-0001"
  },
  "fromCache": true,
  "success": true,
  "message": "Retrieved from cache"
}
```

## 📊 Test Results

```
Test Run Successful.
Total tests: 11
     Passed: 11
```

### Test Coverage
- ✅ Cache service operations
- ✅ MediatR handlers
- ✅ Cache hit/miss scenarios
- ✅ Error handling
- ✅ TTL expiration

## 🔧 Configuration

### Cache Settings (appsettings.json)
```json
{
  "CacheSettings": {
    "DefaultExpiration": "00:30:00",
    "Provider": "InMemory"
  }
}
```

### Default Cache Expiration Times
- Product Details: 15 minutes
- Product Categories: 10 minutes
- Shopping Cart: 24 hours
- Default: 30 minutes

## 🛠️ Technologies Used

- **Framework**: .NET 8.0
- **Architecture**: Clean Architecture
- **Caching**: Microsoft.Extensions.Caching.Memory
- **Mediator**: MediatR 12.2.0
- **Testing**: xUnit, Moq
- **API Documentation**: Swagger/OpenAPI
- **Dependency Injection**: Built-in .NET DI

## 📦 NuGet Packages

### Application Layer
- MediatR (12.2.0)
- Microsoft.Extensions.Logging.Abstractions (8.0.0)

### Infrastructure Layer
- Microsoft.Extensions.Caching.Memory (8.0.0)
- Microsoft.Extensions.Caching.StackExchangeRedis (8.0.0)
- Microsoft.Extensions.Configuration.Abstractions (8.0.0)

### Test Projects
- xUnit (2.5.3)
- Moq (4.20.70)
- Microsoft.NET.Test.Sdk (17.8.0)

## 🚦 Project Structure

```
CleanArchitectureCache.sln
├── src/
│   ├── Domain/
│   │   ├── Common/
│   │   │   └── BaseEntity.cs
│   │   ├── Entities/
│   │   │   ├── Product.cs
│   │   │   ├── Order.cs
│   │   │   └── User.cs
│   │   └── ValueObjects/
│   ├── Application/
│   │   ├── Common/
│   │   │   └── CacheKeys.cs
│   │   ├── Contracts/
│   │   │   ├── Infrastructure/
│   │   │   │   └── ICacheService.cs
│   │   │   └── Persistence/
│   │   │       ├── IGenericRepository.cs
│   │   │       ├── IProductRepository.cs
│   │   │       └── IOrderRepository.cs
│   │   ├── Features/
│   │   │   └── Products/
│   │   │       └── Queries/
│   │   │           ├── GetProductDetailsQuery.cs
│   │   │           └── GetProductsByCategoryQuery.cs
│   │   ├── ViewModels/
│   │   │   ├── ProductVm.cs
│   │   │   ├── OrderVm.cs
│   │   │   ├── UserVm.cs
│   │   │   ├── CartVm.cs
│   │   │   └── CacheDataModels.cs
│   │   └── ApplicationServiceRegistration.cs
│   ├── Infrastructure/
│   │   ├── Cache/
│   │   │   ├── ICacheProvider.cs
│   │   │   ├── InMemoryCacheProvider.cs
│   │   │   └── CacheService.cs
│   │   └── InfrastructureServiceRegistration.cs
│   └── API/
│       ├── Controllers/
│       │   ├── ProductsController.cs
│       │   └── CartController.cs
│       ├── Properties/
│       │   └── launchSettings.json
│       ├── appsettings.json
│       └── Program.cs
└── tests/
    └── Application.Tests/
        ├── Features/
        │   └── Products/
        │       └── Queries/
        │           └── GetProductDetailsQueryTests.cs
        └── Infrastructure/
            └── Cache/
                └── CacheServiceTests.cs
```

## 🔮 Future Enhancements

- [ ] Add Redis cache provider
- [ ] Implement distributed caching
- [ ] Add cache statistics endpoint
- [ ] Implement cache warming
- [ ] Add GraphQL support
- [ ] Create frontend UI
- [ ] Add integration tests
- [ ] Implement cache invalidation strategies
- [ ] Add performance monitoring
- [ ] Docker support

## 📝 Notes

- The project uses in-memory caching for simplicity
- Product data is simulated (no actual database)
- Cache keys follow pattern: `{entity}:{identifier}:{type}`
- All cache operations include error handling and logging
- MediatR pattern ensures clean separation of concerns

## 📄 Additional Documentation

For comprehensive caching concepts and patterns, see:
- `CACHE_DOCUMENTATION.md` - Detailed caching patterns, strategies, and implementation guide

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch
3. Commit your changes
4. Push to the branch
5. Open a Pull Request

## 📜 License

This project is for educational purposes.

---

**Built with Clean Architecture principles and ❤️ using .NET 8**
