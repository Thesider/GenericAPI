using GenericAPI.Models;

namespace GenericAPI.Repositories;

public interface IOrderRepository : IGenericRepository<Order>
{
    Task<Order?> GetOrderWithDetailsAsync(int orderId);
    Task<IEnumerable<Order>> GetUserOrdersAsync(int userId);
    Task<IEnumerable<Order>> GetOrdersByStatusAsync(string status);
    Task<bool> UpdateOrderStatusAsync(int orderId, string status);
    Task<decimal> GetTotalRevenueAsync(DateTime? startDate = null, DateTime? endDate = null);
    Task<IEnumerable<Order>> GetRecentOrdersAsync(int count = 10);
    Task<(int Pending, int Processing, int Shipped, int Delivered, int Cancelled)> GetOrderStatusCountsAsync();
    Task<bool> CancelOrderAsync(int orderId);
}
