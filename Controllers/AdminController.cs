using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using GenericAPI.Services;
using GenericAPI.DTOs;
using GenericAPI.Repositories;

namespace GenericAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class AdminController : ControllerBase
{
    private readonly IProductService _productService;
    private readonly IOrderService _orderService;
    private readonly IUserRepository _userRepository;
    private readonly ILogger<AdminController> _logger;

    public AdminController(
        IProductService productService,
        IOrderService orderService,
        IUserRepository userRepository,
        ILogger<AdminController> logger)
    {
        _productService = productService;
        _orderService = orderService;
        _userRepository = userRepository;
        _logger = logger;
    }

    /// <summary>
    /// Get dashboard statistics
    /// </summary>
    [HttpGet("dashboard")]
    public async Task<ActionResult> GetDashboardStatistics()
    {
        try
        {
            // Get order statistics
            var orderStats = await _orderService.GetOrderStatusCountsAsync();
            
            // Get product statistics
            var allProducts = await _productService.GetAllProductsAsync();
            var lowStockProducts = await _productService.GetProductsWithLowStockAsync(10);
            var inStockProducts = await _productService.GetProductsInStockAsync();
            
            // Get recent orders
            var recentOrders = await _orderService.GetRecentOrdersAsync(5);
            
            // Get total revenue for this month
            var startOfMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            var monthlyRevenue = await _orderService.GetTotalRevenueAsync(startOfMonth, DateTime.Now);
            
            // Get total revenue for last 30 days
            var last30Days = DateTime.Now.AddDays(-30);
            var last30DaysRevenue = await _orderService.GetTotalRevenueAsync(last30Days, DateTime.Now);

            var dashboard = new
            {
                orderStatistics = orderStats,
                productStatistics = new
                {
                    totalProducts = allProducts.Count(),
                    activeProducts = allProducts.Count(p => p.IsActive),
                    inStockProducts = inStockProducts.Count(),
                    lowStockProducts = lowStockProducts.Count(),
                    outOfStockProducts = allProducts.Count(p => p.StockQuantity == 0)
                },
                revenueStatistics = new
                {
                    monthlyRevenue,
                    last30DaysRevenue,
                    currentMonth = startOfMonth.ToString("MMMM yyyy")
                },
                recentOrders,
                lowStockProducts = lowStockProducts.Take(5)
            };

            return Ok(dashboard);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving dashboard statistics");
            return StatusCode(500, new { message = "An error occurred while retrieving dashboard data" });
        }
    }

    /// <summary>
    /// Get all users (Admin only)
    /// </summary>
    [HttpGet("users")]
    public async Task<ActionResult> GetAllUsers([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        try
        {
            if (page < 1 || pageSize < 1 || pageSize > 100)
            {
                return BadRequest(new { message = "Invalid pagination parameters" });
            }

            var allUsers = await _userRepository.GetAllAsync();
            var totalCount = allUsers.Count();
            
            var users = allUsers
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(u => new
                {
                    u.Id,
                    u.Username,
                    u.Email,
                    u.FirstName,
                    u.LastName,
                    u.Role,
                    u.IsActive,                    u.CreatedAt,
                    u.UpdatedAt
                });

            Response.Headers["X-Total-Count"] = totalCount.ToString();
            Response.Headers["X-Page"] = page.ToString();
            Response.Headers["X-Page-Size"] = pageSize.ToString();

            return Ok(new
            {
                data = users,
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
            _logger.LogError(ex, "Error retrieving users");
            return StatusCode(500, new { message = "An error occurred while retrieving users" });
        }
    }

    /// <summary>
    /// Get user by ID
    /// </summary>
    [HttpGet("users/{id}")]
    public async Task<ActionResult> GetUser(int id)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(id);
            if (user == null)
                return NotFound(new { message = $"User with ID {id} not found" });

            var userInfo = new
            {
                user.Id,
                user.Username,
                user.Email,
                user.FirstName,
                user.LastName,
                user.Role,
                user.IsActive,
                user.CreatedAt,
                user.UpdatedAt,
                orderCount = user.Orders?.Count ?? 0
            };

            return Ok(userInfo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user with ID {UserId}", id);
            return StatusCode(500, new { message = "An error occurred while retrieving the user" });
        }
    }

    /// <summary>
    /// Update user status (activate/deactivate)
    /// </summary>
    [HttpPatch("users/{id}/status")]
    public async Task<ActionResult> UpdateUserStatus(int id, [FromBody] UpdateUserStatusDto statusDto)
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

            var user = await _userRepository.GetByIdAsync(id);
            if (user == null)
                return NotFound(new { message = $"User with ID {id} not found" });

            user.IsActive = statusDto.IsActive;
            user.UpdatedAt = DateTime.UtcNow;

            _userRepository.Update(user);
            await _userRepository.SaveChangesAsync();

            var action = statusDto.IsActive ? "activated" : "deactivated";
            _logger.LogInformation("User {UserId} has been {Action}", id, action);

            return Ok(new { message = $"User has been {action} successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user status for user {UserId}", id);
            return StatusCode(500, new { message = "An error occurred while updating user status" });
        }
    }

    /// <summary>
    /// Update user role
    /// </summary>
    [HttpPatch("users/{id}/role")]
    public async Task<ActionResult> UpdateUserRole(int id, [FromBody] UpdateUserRoleDto roleDto)
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

            var validRoles = new[] { "User", "Admin" };
            if (!validRoles.Contains(roleDto.Role))
            {
                return BadRequest(new { message = "Invalid role. Valid roles are: " + string.Join(", ", validRoles) });
            }

            var user = await _userRepository.GetByIdAsync(id);
            if (user == null)
                return NotFound(new { message = $"User with ID {id} not found" });

            user.Role = roleDto.Role;
            user.UpdatedAt = DateTime.UtcNow;

            _userRepository.Update(user);
            await _userRepository.SaveChangesAsync();

            _logger.LogInformation("User {UserId} role updated to {Role}", id, roleDto.Role);

            return Ok(new { message = "User role updated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user role for user {UserId}", id);
            return StatusCode(500, new { message = "An error occurred while updating user role" });
        }
    }

    /// <summary>
    /// Get system analytics
    /// </summary>
    [HttpGet("analytics")]
    public async Task<ActionResult> GetAnalytics(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        try
        {
            // Default to last 30 days if no dates provided
            endDate ??= DateTime.Now;
            startDate ??= endDate.Value.AddDays(-30);

            if (startDate > endDate)
            {
                return BadRequest(new { message = "Start date cannot be after end date" });
            }

            // Get revenue for the period
            var totalRevenue = await _orderService.GetTotalRevenueAsync(startDate, endDate);
            
            // Get order statistics
            var orderStats = await _orderService.GetOrderStatusCountsAsync();
            
            // Get all orders to calculate analytics
            var allOrders = await _orderService.GetAllOrdersAsync();
            var periodOrders = allOrders.Where(o => o.OrderDate >= startDate && o.OrderDate <= endDate);
            
            // Calculate analytics
            var analytics = new
            {
                period = new
                {
                    startDate = startDate.Value.ToString("yyyy-MM-dd"),
                    endDate = endDate.Value.ToString("yyyy-MM-dd"),
                    days = (endDate.Value - startDate.Value).Days + 1
                },
                revenue = new
                {
                    total = totalRevenue,
                    average = periodOrders.Any() ? totalRevenue / periodOrders.Count() : 0,
                    dailyAverage = totalRevenue / ((endDate.Value - startDate.Value).Days + 1)
                },
                orders = new
                {
                    total = periodOrders.Count(),
                    completed = periodOrders.Count(o => o.Status == "Delivered"),
                    cancelled = periodOrders.Count(o => o.Status == "Cancelled"),
                    pending = periodOrders.Count(o => o.Status == "Pending"),
                    processing = periodOrders.Count(o => o.Status == "Processing"),
                    shipped = periodOrders.Count(o => o.Status == "Shipped")
                },
                products = new
                {
                    totalActive = (await _productService.GetAllProductsAsync()).Count(),
                    lowStock = (await _productService.GetProductsWithLowStockAsync()).Count(),
                    outOfStock = (await _productService.GetAllProductsAsync()).Count(p => p.StockQuantity == 0)
                },
                customers = new
                {
                    totalActive = (await _userRepository.GetAllAsync()).Count(u => u.IsActive),
                    totalInactive = (await _userRepository.GetAllAsync()).Count(u => !u.IsActive),
                    newCustomers = (await _userRepository.GetAllAsync()).Count(u => u.CreatedAt >= startDate && u.CreatedAt <= endDate)
                }
            };

            return Ok(analytics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving analytics");
            return StatusCode(500, new { message = "An error occurred while retrieving analytics" });
        }
    }

    /// <summary>
    /// Get system health status
    /// </summary>
    [HttpGet("health")]
    public async Task<ActionResult> GetSystemHealth()
    {
        try
        {
            var health = new
            {
                status = "Healthy",
                timestamp = DateTime.UtcNow,
                version = "1.0.0",
                uptime = DateTime.UtcNow.Subtract(System.Diagnostics.Process.GetCurrentProcess().StartTime).ToString(@"dd\:hh\:mm\:ss"),
                database = await CheckDatabaseHealth(),
                services = new
                {
                    productService = "Healthy",
                    orderService = "Healthy",
                    userService = "Healthy"
                }
            };

            return Ok(health);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking system health");
            return StatusCode(500, new { message = "An error occurred while checking system health" });
        }
    }

    private async Task<string> CheckDatabaseHealth()
    {
        try
        {
            // Simple database connectivity check
            var users = await _userRepository.GetAllAsync();
            return "Connected";
        }
        catch
        {
            return "Disconnected";
        }
    }
}

// Additional DTOs for Admin operations
public class UpdateUserStatusDto
{
    public bool IsActive { get; set; }
}

public class UpdateUserRoleDto
{
    public required string Role { get; set; }
}
