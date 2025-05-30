using GenericAPI.DTOs;
using GenericAPI.Models;

namespace GenericAPI.Services;

public interface IOrderService
{
    Task<IEnumerable<OrderDto>> GetAllOrdersAsync();
    Task<OrderDto?> GetOrderByIdAsync(int id);
    Task<OrderDto?> GetOrderWithDetailsAsync(int id);
    Task<IEnumerable<OrderDto>> GetUserOrdersAsync(int userId);
    Task<IEnumerable<OrderDto>> GetOrdersByStatusAsync(string status);
    Task<OrderDto> CreateOrderAsync(CreateOrderDto createOrderDto);
    Task<bool> UpdateOrderStatusAsync(int id, string status);
    Task<bool> CancelOrderAsync(int id);
    Task<decimal> GetTotalRevenueAsync(DateTime? startDate = null, DateTime? endDate = null);
    Task<IEnumerable<OrderDto>> GetRecentOrdersAsync(int count = 10);
    Task<OrderStatusSummaryDto> GetOrderStatusCountsAsync();
    Task<(IEnumerable<OrderDto> Orders, int TotalCount)> GetPagedOrdersAsync(int page, int pageSize, string? status = null);
}
