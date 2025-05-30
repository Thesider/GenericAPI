using AutoMapper;
using GenericAPI.DTOs;
using GenericAPI.Middleware;
using GenericAPI.Models;
using GenericAPI.Repositories;

namespace GenericAPI.Services;

public class OrderService : IOrderService
{
    private readonly IOrderRepository _orderRepository;
    private readonly IProductRepository _productRepository;
    private readonly IUserRepository _userRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<OrderService> _logger;

    public OrderService(
        IOrderRepository orderRepository,
        IProductRepository productRepository,
        IUserRepository userRepository,
        IMapper mapper,
        ILogger<OrderService> logger)
    {
        _orderRepository = orderRepository;
        _productRepository = productRepository;
        _userRepository = userRepository;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<IEnumerable<OrderDto>> GetAllOrdersAsync()
    {
        try
        {
            var orders = await _orderRepository.GetAllAsync();
            return _mapper.Map<IEnumerable<OrderDto>>(orders);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all orders");
            throw;
        }
    }

    public async Task<OrderDto?> GetOrderByIdAsync(int id)
    {
        try
        {
            var order = await _orderRepository.GetByIdAsync(id);
            return order != null ? _mapper.Map<OrderDto>(order) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving order with ID {OrderId}", id);
            throw;
        }
    }

    public async Task<OrderDto?> GetOrderWithDetailsAsync(int id)
    {
        try
        {
            var order = await _orderRepository.GetOrderWithDetailsAsync(id);
            return order != null ? _mapper.Map<OrderDto>(order) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving order details with ID {OrderId}", id);
            throw;
        }
    }

    public async Task<IEnumerable<OrderDto>> GetUserOrdersAsync(int userId)
    {
        try
        {
            var orders = await _orderRepository.GetUserOrdersAsync(userId);
            return _mapper.Map<IEnumerable<OrderDto>>(orders);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving orders for user ID {UserId}", userId);
            throw;
        }
    }

    public async Task<IEnumerable<OrderDto>> GetOrdersByStatusAsync(string status)
    {
        try
        {
            var orders = await _orderRepository.GetOrdersByStatusAsync(status);
            return _mapper.Map<IEnumerable<OrderDto>>(orders);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving orders with status {Status}", status);
            throw;
        }
    }

    public async Task<OrderDto> CreateOrderAsync(CreateOrderDto createOrderDto)
    {
        try
        {
            if (!createOrderDto.OrderItems.Any())
                throw new ValidationException("Order must contain at least one item");

            // Validate all products exist and calculate total
            decimal totalAmount = 0;
            var orderItems = new List<OrderItem>();

            foreach (var item in createOrderDto.OrderItems)
            {
                var product = await _productRepository.GetByIdAsync(item.ProductId);
                if (product == null)
                    throw new ValidationException($"Product with ID {item.ProductId} not found");

                if (!product.IsActive)
                    throw new ValidationException($"Product {product.Name} is not available");

                if (product.StockQuantity < item.Quantity)
                    throw new ValidationException($"Insufficient stock for product {product.Name}. Available: {product.StockQuantity}, Requested: {item.Quantity}");

                var orderItem = new OrderItem
                {
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    UnitPrice = product.Price
                };

                orderItems.Add(orderItem);
                totalAmount += orderItem.TotalPrice;

                // Update product stock
                await _productRepository.UpdateStockAsync(product.Id, product.StockQuantity - item.Quantity);
            }

            var order = new Order
            {
                UserId = createOrderDto.UserId ?? throw new ValidationException("User ID is required"),
                OrderDate = DateTime.UtcNow,
                Status = "Pending",
                ShippingAddress = createOrderDto.ShippingAddress,
                TotalAmount = totalAmount,
                OrderItems = orderItems,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var createdOrder = await _orderRepository.AddAsync(order);
            await _orderRepository.SaveChangesAsync();

            _logger.LogInformation("Order created with ID {OrderId} for user {UserId}", createdOrder.Id, order.UserId);
            return _mapper.Map<OrderDto>(createdOrder);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating order");
            throw;
        }
    }

    public async Task<bool> UpdateOrderStatusAsync(int id, string status)
    {
        try
        {
            var validStatuses = new[] { "Pending", "Processing", "Shipped", "Delivered", "Cancelled" };
            if (!validStatuses.Contains(status))
                throw new ValidationException($"Invalid status. Valid statuses are: {string.Join(", ", validStatuses)}");

            var success = await _orderRepository.UpdateOrderStatusAsync(id, status);
            if (success)
            {
                _logger.LogInformation("Order status updated for ID {OrderId} to {Status}", id, status);
            }
            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating order status for ID {OrderId}", id);
            throw;
        }
    }

    public async Task<bool> CancelOrderAsync(int id)
    {
        try
        {
            var order = await _orderRepository.GetOrderWithDetailsAsync(id);
            if (order == null)
                return false;

            if (order.Status == "Delivered" || order.Status == "Cancelled")
                throw new BusinessRuleException($"Cannot cancel order with status {order.Status}");

            // Restore product stock
            foreach (var item in order.OrderItems)
            {
                var product = await _productRepository.GetByIdAsync(item.ProductId);
                if (product != null)
                {
                    await _productRepository.UpdateStockAsync(product.Id, product.StockQuantity + item.Quantity);
                }
            }

            var success = await _orderRepository.CancelOrderAsync(id);
            if (success)
            {
                _logger.LogInformation("Order cancelled with ID {OrderId}", id);
            }
            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling order with ID {OrderId}", id);
            throw;
        }
    }

    public async Task<decimal> GetTotalRevenueAsync(DateTime? startDate = null, DateTime? endDate = null)
    {
        try
        {
            return await _orderRepository.GetTotalRevenueAsync(startDate, endDate);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating total revenue");
            throw;
        }
    }

    public async Task<IEnumerable<OrderDto>> GetRecentOrdersAsync(int count = 10)
    {
        try
        {
            var orders = await _orderRepository.GetRecentOrdersAsync(count);
            return _mapper.Map<IEnumerable<OrderDto>>(orders);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving recent orders");
            throw;
        }
    }

    public async Task<OrderStatusSummaryDto> GetOrderStatusCountsAsync()
    {
        try
        {
            var counts = await _orderRepository.GetOrderStatusCountsAsync();
            return new OrderStatusSummaryDto
            {
                Pending = counts.Pending,
                Processing = counts.Processing,
                Shipped = counts.Shipped,
                Delivered = counts.Delivered,
                Cancelled = counts.Cancelled
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving order status counts");
            throw;
        }
    }

    public async Task<(IEnumerable<OrderDto> Orders, int TotalCount)> GetPagedOrdersAsync(int page, int pageSize, string? status = null)
    {
        try
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 10;
            if (pageSize > 100) pageSize = 100;

            var orders = string.IsNullOrEmpty(status) 
                ? await _orderRepository.GetAllAsync()
                : await _orderRepository.GetOrdersByStatusAsync(status);

            var totalCount = orders.Count();
            var pagedOrders = orders
                .Skip((page - 1) * pageSize)
                .Take(pageSize);

            var orderDtos = _mapper.Map<IEnumerable<OrderDto>>(pagedOrders);
            
            return (orderDtos, totalCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving paged orders");
            throw;
        }
    }
}
