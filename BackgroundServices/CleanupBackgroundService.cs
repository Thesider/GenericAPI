using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using GenericAPI.Repositories;
using GenericAPI.Services;

namespace GenericAPI.BackgroundServices
{
    /// <summary>
    /// Background service for cleanup tasks and maintenance
    /// </summary>
    public class CleanupBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<CleanupBackgroundService> _logger;
        private readonly TimeSpan _cleanupInterval = TimeSpan.FromHours(24); // Run daily

        public CleanupBackgroundService(
            IServiceProvider serviceProvider,
            ILogger<CleanupBackgroundService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Cleanup Background Service started");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await PerformCleanupTasksAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred during cleanup tasks");
                }

                await Task.Delay(_cleanupInterval, stoppingToken);
            }

            _logger.LogInformation("Cleanup Background Service stopped");
        }

        private async Task PerformCleanupTasksAsync()
        {
            _logger.LogInformation("Starting cleanup tasks...");

            using var scope = _serviceProvider.CreateScope();
            var orderRepository = scope.ServiceProvider.GetRequiredService<IOrderRepository>();
            var productRepository = scope.ServiceProvider.GetRequiredService<IProductRepository>();
            var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();

            // Cleanup expired pending orders (older than 30 minutes)
            await CleanupExpiredPendingOrdersAsync(orderRepository);

            // Check for low stock products
            await CheckLowStockProductsAsync(productRepository, notificationService);

            // Send daily summary notifications to admins
            await SendDailySummaryNotificationsAsync(orderRepository, productRepository, notificationService);

            _logger.LogInformation("Cleanup tasks completed");
        }        private async Task CleanupExpiredPendingOrdersAsync(IOrderRepository orderRepository)
        {
            try
            {
                var expiredTime = DateTime.UtcNow.AddMinutes(-30);
                var pendingOrders = await orderRepository.GetOrdersByStatusAsync("Pending");
                var expiredOrders = pendingOrders.Where(o => o.OrderDate < expiredTime).ToList();

                foreach (var order in expiredOrders)
                {
                    order.Status = "Cancelled";
                    order.UpdatedAt = DateTime.UtcNow;
                    orderRepository.Update(order);
                }

                if (expiredOrders.Any())
                {
                    await orderRepository.SaveChangesAsync();
                    _logger.LogInformation("Cancelled {Count} expired pending orders", expiredOrders.Count());
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cleaning up expired pending orders");
            }
        }        private async Task CheckLowStockProductsAsync(IProductRepository productRepository, INotificationService notificationService)
        {
            try
            {
                var lowStockThreshold = 10; // Configure this value as needed
                var allProducts = await productRepository.GetAllAsync();
                var lowStockProducts = allProducts.Where(p => p.StockQuantity <= lowStockThreshold && p.IsActive).ToList();

                foreach (var product in lowStockProducts)
                {
                    await notificationService.SendLowStockNotificationAsync(
                        product.Id, 
                        product.Name, 
                        product.StockQuantity);
                }

                if (lowStockProducts.Any())
                {
                    _logger.LogInformation("Sent low stock notifications for {Count} products", lowStockProducts.Count);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking low stock products");
            }
        }        private async Task SendDailySummaryNotificationsAsync(
            IOrderRepository orderRepository, 
            IProductRepository productRepository, 
            INotificationService notificationService)
        {
            try
            {
                var today = DateTime.UtcNow.Date;
                var tomorrow = today.AddDays(1);

                // Get today's statistics - using FindAsync to filter by date range
                var todaysOrders = await orderRepository.FindAsync(o => o.OrderDate >= today && o.OrderDate < tomorrow);
                var orderCount = todaysOrders.Count();
                var revenue = todaysOrders.Sum(o => o.TotalAmount);

                var activeProductCount = (await productRepository.GetAllAsync()).Count(p => p.IsActive);

                // Send summary to all admin users
                var message = $"Today's Summary: {orderCount} orders, ${revenue:F2} revenue, {activeProductCount} active products";
                
                // This would require getting admin user IDs - for now we'll log it
                _logger.LogInformation("Daily Summary: {OrderCount} orders, ${Revenue:F2} revenue, {ProductCount} active products", 
                    orderCount, revenue, activeProductCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending daily summary notifications");
            }
        }
    }
}
