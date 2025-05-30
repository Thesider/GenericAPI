using GenericAPI.Data;
using GenericAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace GenericAPI.Repositories;

public class OrderRepository : GenericRepository<Order>, IOrderRepository
{
    public OrderRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<Order?> GetOrderWithDetailsAsync(int orderId)
    {
        return await _dbSet
            .Include(o => o.User)
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
            .FirstOrDefaultAsync(o => o.Id == orderId);
    }

    public async Task<IEnumerable<Order>> GetUserOrdersAsync(int userId)
    {
        return await _dbSet
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
            .Where(o => o.UserId == userId)
            .OrderByDescending(o => o.OrderDate)
            .ToListAsync();
    }

    public async Task<IEnumerable<Order>> GetOrdersByStatusAsync(string status)
    {
        return await _dbSet
            .Include(o => o.User)
            .Include(o => o.OrderItems)
            .Where(o => o.Status == status)
            .OrderByDescending(o => o.OrderDate)
            .ToListAsync();
    }

    public async Task<bool> UpdateOrderStatusAsync(int orderId, string status)
    {
        var order = await _dbSet.FindAsync(orderId);
        if (order == null)
            return false;

        order.Status = status;
        order.UpdatedAt = DateTime.UtcNow;
        
        if (status == "Delivered")
            order.CompletedAt = DateTime.UtcNow;

        return await _context.SaveChangesAsync() > 0;
    }

    public async Task<decimal> GetTotalRevenueAsync(DateTime? startDate = null, DateTime? endDate = null)
    {
        var query = _dbSet.AsQueryable();

        if (startDate.HasValue)
            query = query.Where(o => o.OrderDate >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(o => o.OrderDate <= endDate.Value);

        return await query
            .Where(o => o.Status == "Delivered")
            .SumAsync(o => o.TotalAmount);
    }

    public async Task<IEnumerable<Order>> GetRecentOrdersAsync(int count = 10)
    {
        return await _dbSet
            .Include(o => o.User)
            .Include(o => o.OrderItems)
            .OrderByDescending(o => o.OrderDate)
            .Take(count)
            .ToListAsync();
    }

    public async Task<(int Pending, int Processing, int Shipped, int Delivered, int Cancelled)> GetOrderStatusCountsAsync()
    {
        var counts = await _dbSet
            .GroupBy(o => o.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Status, x => x.Count);

        return (
            Pending: counts.GetValueOrDefault("Pending"),
            Processing: counts.GetValueOrDefault("Processing"),
            Shipped: counts.GetValueOrDefault("Shipped"),
            Delivered: counts.GetValueOrDefault("Delivered"),
            Cancelled: counts.GetValueOrDefault("Cancelled")
        );
    }

    public async Task<bool> CancelOrderAsync(int orderId)
    {
        var order = await _dbSet
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
            .FirstOrDefaultAsync(o => o.Id == orderId);

        if (order == null || order.Status == "Delivered" || order.Status == "Cancelled")
            return false;

        // Restore product stock quantities
        foreach (var item in order.OrderItems)
        {
            item.Product.StockQuantity += item.Quantity;
            item.Product.UpdatedAt = DateTime.UtcNow;
        }

        order.Status = "Cancelled";
        order.UpdatedAt = DateTime.UtcNow;

        return await _context.SaveChangesAsync() > 0;
    }

    public override async Task<Order?> GetByIdAsync(int id)
    {
        return await _dbSet
            .Include(o => o.OrderItems)
            .FirstOrDefaultAsync(o => o.Id == id);
    }
}
