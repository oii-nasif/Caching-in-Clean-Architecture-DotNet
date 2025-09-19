using Domain.Entities;

namespace Application.Contracts.Persistence;

public interface IProductRepository : IGenericRepository<Product>
{
    Task<IReadOnlyList<Product>> GetProductsByCategoryAsync(string category);
    Task<Product?> GetProductBySkuAsync(string sku);
    Task<bool> UpdateStockAsync(int productId, int quantity);
}