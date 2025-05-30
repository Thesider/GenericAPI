using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using GenericAPI.Services;
using GenericAPI.DTOs;
using System.Security.Claims;

namespace GenericAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class OrderController : ControllerBase
{
    private readonly IOrderService _orderService;
    private readonly ILogger<OrderController> _logger;

    public OrderController(IOrderService orderService, ILogger<OrderController> logger)
    {
        _orderService = orderService;
        _logger = logger;
    }

    /// <summary>
    /// Get all orders with pagination (Admin only)
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<IEnumerable<OrderDto>>> GetOrders(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? status = null)
    {
        try
        {
            if (page < 1 || pageSize < 1 || pageSize > 100)
            {
                return BadRequest(new { message = "Invalid pagination parameters" });
            }            var (orders, totalCount) = await _orderService.GetPagedOrdersAsync(page, pageSize, status);
            
            Response.Headers["X-Total-Count"] = totalCount.ToString();
            Response.Headers["X-Page"] = page.ToString();
            Response.Headers["X-Page-Size"] = pageSize.ToString();

            return Ok(new
            {
                data = orders,
                pagination = new
                {
                    currentPage = page,
                    pageSize,
                    totalCount,
                    totalPages = (int)Math.Ceiling((double)totalCount / pageSize)
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving orders");
            return StatusCode(500, new { message = "An error occurred while retrieving orders" });
        }
    }

    /// <summary>
    /// Get all orders (Admin only)
    /// </summary>
    [HttpGet("all")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<IEnumerable<OrderDto>>> GetAllOrders()
    {
        try
        {
            var orders = await _orderService.GetAllOrdersAsync();
            return Ok(orders);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all orders");
            return StatusCode(500, new { message = "An error occurred while retrieving orders" });
        }
    }

    /// <summary>
    /// Get a specific order by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<OrderDto>> GetOrder(int id)
    {
        try
        {
            var order = await _orderService.GetOrderWithDetailsAsync(id);
            if (order == null)
                return NotFound(new { message = $"Order with ID {id} not found" });

            // Users can only access their own orders, unless they're admin
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            
            if (userRole != "Admin" && userIdClaim != null)
            {
                if (int.TryParse(userIdClaim, out int userId) && order.UserId != userId)
                {
                    return Forbid();
                }
            }

            return Ok(order);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving order with ID {OrderId}", id);
            return StatusCode(500, new { message = "An error occurred while retrieving the order" });
        }
    }

    /// <summary>
    /// Get orders for the current user
    /// </summary>
    [HttpGet("my-orders")]
    public async Task<ActionResult<IEnumerable<OrderDto>>> GetMyOrders()
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userIdClaim == null || !int.TryParse(userIdClaim, out int userId))
            {
                return BadRequest(new { message = "Invalid user ID" });
            }

            var orders = await _orderService.GetUserOrdersAsync(userId);
            return Ok(orders);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user orders");
            return StatusCode(500, new { message = "An error occurred while retrieving your orders" });
        }
    }

    /// <summary>
    /// Get orders by status (Admin only)
    /// </summary>
    [HttpGet("status/{status}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<IEnumerable<OrderDto>>> GetOrdersByStatus(string status)
    {
        try
        {
            var validStatuses = new[] { "Pending", "Processing", "Shipped", "Delivered", "Cancelled" };
            if (!validStatuses.Contains(status, StringComparer.OrdinalIgnoreCase))
            {
                return BadRequest(new { message = "Invalid status. Valid statuses are: " + string.Join(", ", validStatuses) });
            }

            var orders = await _orderService.GetOrdersByStatusAsync(status);
            return Ok(orders);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving orders by status {Status}", status);
            return StatusCode(500, new { message = "An error occurred while retrieving orders" });
        }
    }

    /// <summary>
    /// Get recent orders (Admin only)
    /// </summary>
    [HttpGet("recent")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<IEnumerable<OrderDto>>> GetRecentOrders([FromQuery] int count = 10)
    {
        try
        {
            if (count < 1 || count > 100)
            {
                return BadRequest(new { message = "Count must be between 1 and 100" });
            }

            var orders = await _orderService.GetRecentOrdersAsync(count);
            return Ok(orders);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving recent orders");
            return StatusCode(500, new { message = "An error occurred while retrieving recent orders" });
        }
    }

    /// <summary>
    /// Create a new order
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<OrderDto>> CreateOrder(CreateOrderDto createOrderDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new 
                { 
                    message = "Validation failed", 
                    errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                });
            }

            // Set user ID from claims if not provided (for regular users)
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            
            if (userRole != "Admin")
            {
                if (userIdClaim == null || !int.TryParse(userIdClaim, out int userId))
                {
                    return BadRequest(new { message = "Invalid user ID" });
                }
                createOrderDto.UserId = userId;
            }
            else if (createOrderDto.UserId == null)
            {
                return BadRequest(new { message = "User ID is required for admin-created orders" });
            }

            var order = await _orderService.CreateOrderAsync(createOrderDto);
            return CreatedAtAction(nameof(GetOrder), new { id = order.Id }, order);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating order");
            return StatusCode(500, new { message = "An error occurred while creating the order" });
        }
    }

    /// <summary>
    /// Update order status (Admin only)
    /// </summary>
    [HttpPatch("{id}/status")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> UpdateOrderStatus(int id, UpdateOrderStatusDto updateStatusDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new 
                { 
                    message = "Validation failed", 
                    errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                });
            }

            var validStatuses = new[] { "Pending", "Processing", "Shipped", "Delivered", "Cancelled" };
            if (!validStatuses.Contains(updateStatusDto.Status, StringComparer.OrdinalIgnoreCase))
            {
                return BadRequest(new { message = "Invalid status. Valid statuses are: " + string.Join(", ", validStatuses) });
            }

            var success = await _orderService.UpdateOrderStatusAsync(id, updateStatusDto.Status);
            if (!success)
                return NotFound(new { message = $"Order with ID {id} not found" });

            return Ok(new { message = "Order status updated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating order status for order {OrderId}", id);
            return StatusCode(500, new { message = "An error occurred while updating order status" });
        }
    }

    /// <summary>
    /// Cancel an order
    /// </summary>
    [HttpPatch("{id}/cancel")]
    public async Task<ActionResult> CancelOrder(int id)
    {
        try
        {
            // Check if user can cancel this order
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            
            if (userRole != "Admin" && userIdClaim != null)
            {
                if (int.TryParse(userIdClaim, out int userId))
                {
                    var order = await _orderService.GetOrderByIdAsync(id);
                    if (order == null)
                        return NotFound(new { message = $"Order with ID {id} not found" });
                    
                    if (order.UserId != userId)
                        return Forbid();
                }
            }

            var success = await _orderService.CancelOrderAsync(id);
            if (!success)
                return NotFound(new { message = $"Order with ID {id} not found or cannot be cancelled" });

            return Ok(new { message = "Order cancelled successfully" });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling order {OrderId}", id);
            return StatusCode(500, new { message = "An error occurred while cancelling the order" });
        }
    }

    /// <summary>
    /// Get order statistics summary (Admin only)
    /// </summary>
    [HttpGet("statistics")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<OrderStatusSummaryDto>> GetOrderStatistics()
    {
        try
        {
            var statistics = await _orderService.GetOrderStatusCountsAsync();
            return Ok(statistics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving order statistics");
            return StatusCode(500, new { message = "An error occurred while retrieving order statistics" });
        }
    }

    /// <summary>
    /// Get total revenue (Admin only)
    /// </summary>
    [HttpGet("revenue")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> GetRevenue(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        try
        {
            if (startDate.HasValue && endDate.HasValue && startDate > endDate)
            {
                return BadRequest(new { message = "Start date cannot be after end date" });
            }

            var revenue = await _orderService.GetTotalRevenueAsync(startDate, endDate);
            return Ok(new { 
                totalRevenue = revenue,
                period = new {
                    startDate = startDate?.ToString("yyyy-MM-dd"),
                    endDate = endDate?.ToString("yyyy-MM-dd")
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating revenue");
            return StatusCode(500, new { message = "An error occurred while calculating revenue" });
        }
    }
}
