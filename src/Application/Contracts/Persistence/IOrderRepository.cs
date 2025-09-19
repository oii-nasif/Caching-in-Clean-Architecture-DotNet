using Domain.Entities;

namespace Application.Contracts.Persistence;

public interface IOrderRepository : IGenericRepository<Order>
{
    Task<IReadOnlyList<Order>> GetOrdersByCustomerAsync(int customerId);
    Task<Order?> GetOrderByNumberAsync(string orderNumber);
    Task<IReadOnlyList<Order>> GetOrdersByStatusAsync(OrderStatus status);
}