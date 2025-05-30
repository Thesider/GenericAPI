using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using GenericAPI.Repositories;

namespace GenericAPI.Services
{
    /// <summary>
    /// In-memory notification service implementation with cache storage
    /// </summary>
    public class NotificationService : INotificationService
    {
        private readonly ICacheService _cacheService;
        private readonly IUserRepository _userRepository;
        private readonly ILogger<NotificationService> _logger;
        private const string NotificationCachePrefix = "notifications:user:";
        private const string NotificationCountCachePrefix = "notification_count:user:";
        private static readonly TimeSpan CacheExpiration = TimeSpan.FromHours(24);

        public NotificationService(
            ICacheService cacheService,
            IUserRepository userRepository,
            ILogger<NotificationService> logger)
        {
            _cacheService = cacheService;
            _userRepository = userRepository;
            _logger = logger;
        }

        public async Task<string> SendNotificationAsync(string userId, string title, string message,
            NotificationType type = NotificationType.Info, Dictionary<string, object>? data = null)
        {
            var notification = new NotificationMessage
            {
                UserId = userId,
                Title = title,
                Message = message,
                Type = type,
                Data = data ?? new Dictionary<string, object>()
            };

            await StoreNotificationAsync(notification);
            
            _logger.LogInformation("Notification sent to user {UserId}: {Title}", userId, title);
            return notification.Id;
        }

        public async Task<List<string>> SendBulkNotificationAsync(List<string> userIds, string title, string message,
            NotificationType type = NotificationType.Info, Dictionary<string, object>? data = null)
        {
            var notificationIds = new List<string>();

            foreach (var userId in userIds)
            {
                var notificationId = await SendNotificationAsync(userId, title, message, type, data);
                notificationIds.Add(notificationId);
            }

            _logger.LogInformation("Bulk notification sent to {UserCount} users: {Title}", userIds.Count, title);
            return notificationIds;
        }

        public async Task<List<NotificationMessage>> GetUserNotificationsAsync(string userId, bool unreadOnly = false, int limit = 50)
        {
            var cacheKey = $"{NotificationCachePrefix}{userId}";
            var notifications = await _cacheService.GetAsync<List<NotificationMessage>>(cacheKey);

            if (notifications == null)
            {
                notifications = new List<NotificationMessage>();
            }

            var query = notifications.AsQueryable();

            if (unreadOnly)
            {
                query = query.Where(n => !n.IsRead);
            }

            return query
                .OrderByDescending(n => n.CreatedAt)
                .Take(limit)
                .ToList();
        }

        public async Task<bool> MarkAsReadAsync(string notificationId, string userId)
        {
            var cacheKey = $"{NotificationCachePrefix}{userId}";
            var notifications = await _cacheService.GetAsync<List<NotificationMessage>>(cacheKey);

            if (notifications == null)
            {
                return false;
            }

            var notification = notifications.FirstOrDefault(n => n.Id == notificationId && n.UserId == userId);
            if (notification == null)
            {
                return false;
            }

            notification.IsRead = true;
            await _cacheService.SetAsync(cacheKey, notifications, CacheExpiration);

            // Update unread count cache
            await UpdateUnreadCountCacheAsync(userId);

            _logger.LogDebug("Notification {NotificationId} marked as read for user {UserId}", notificationId, userId);
            return true;
        }

        public async Task<int> MarkAllAsReadAsync(string userId)
        {
            var cacheKey = $"{NotificationCachePrefix}{userId}";
            var notifications = await _cacheService.GetAsync<List<NotificationMessage>>(cacheKey);

            if (notifications == null)
            {
                return 0;
            }

            var unreadCount = notifications.Count(n => !n.IsRead);
            
            foreach (var notification in notifications)
            {
                notification.IsRead = true;
            }

            await _cacheService.SetAsync(cacheKey, notifications, CacheExpiration);

            // Update unread count cache
            await UpdateUnreadCountCacheAsync(userId);

            _logger.LogInformation("Marked {Count} notifications as read for user {UserId}", unreadCount, userId);
            return unreadCount;
        }

        public async Task<bool> DeleteNotificationAsync(string notificationId, string userId)
        {
            var cacheKey = $"{NotificationCachePrefix}{userId}";
            var notifications = await _cacheService.GetAsync<List<NotificationMessage>>(cacheKey);

            if (notifications == null)
            {
                return false;
            }

            var notification = notifications.FirstOrDefault(n => n.Id == notificationId && n.UserId == userId);
            if (notification == null)
            {
                return false;
            }

            notifications.Remove(notification);
            await _cacheService.SetAsync(cacheKey, notifications, CacheExpiration);

            // Update unread count cache
            await UpdateUnreadCountCacheAsync(userId);

            _logger.LogDebug("Notification {NotificationId} deleted for user {UserId}", notificationId, userId);
            return true;
        }

        public async Task<int> GetUnreadCountAsync(string userId)
        {
            var countCacheKey = $"{NotificationCountCachePrefix}{userId}";
            var cachedCount = await _cacheService.GetAsync<int?>(countCacheKey);

            if (cachedCount.HasValue)
            {
                return cachedCount.Value;
            }

            var notifications = await GetUserNotificationsAsync(userId, unreadOnly: true);
            var count = notifications.Count;

            await _cacheService.SetAsync(countCacheKey, count, TimeSpan.FromMinutes(5));
            return count;
        }

        public async Task SendOrderStatusNotificationAsync(string userId, int orderId, string status)
        {
            var title = "Order Status Update";
            var message = $"Your order #{orderId} status has been updated to: {status}";
            var data = new Dictionary<string, object>
            {
                ["orderId"] = orderId,
                ["status"] = status
            };

            await SendNotificationAsync(userId, title, message, NotificationType.Info, data);
        }

        public async Task SendLowStockNotificationAsync(int productId, string productName, int currentStock)
        {
            // Get all admin users
            var adminUsers = await _userRepository.GetUsersByRoleAsync("Admin");
            var adminIds = adminUsers.Select(u => u.Id.ToString()).ToList();

            if (!adminIds.Any())
            {
                _logger.LogWarning("No admin users found to send low stock notification");
                return;
            }

            var title = "Low Stock Alert";
            var message = $"Product '{productName}' is running low on stock. Current stock: {currentStock}";
            var data = new Dictionary<string, object>
            {
                ["productId"] = productId,
                ["productName"] = productName,
                ["currentStock"] = currentStock
            };

            await SendBulkNotificationAsync(adminIds, title, message, NotificationType.Warning, data);
        }

        private async Task StoreNotificationAsync(NotificationMessage notification)
        {
            var cacheKey = $"{NotificationCachePrefix}{notification.UserId}";
            var notifications = await _cacheService.GetAsync<List<NotificationMessage>>(cacheKey);

            if (notifications == null)
            {
                notifications = new List<NotificationMessage>();
            }

            notifications.Add(notification);

            // Keep only the latest 100 notifications per user
            if (notifications.Count > 100)
            {
                notifications = notifications
                    .OrderByDescending(n => n.CreatedAt)
                    .Take(100)
                    .ToList();
            }

            await _cacheService.SetAsync(cacheKey, notifications, CacheExpiration);

            // Update unread count cache
            await UpdateUnreadCountCacheAsync(notification.UserId);
        }

        private async Task UpdateUnreadCountCacheAsync(string userId)
        {
            var cacheKey = $"{NotificationCachePrefix}{userId}";
            var notifications = await _cacheService.GetAsync<List<NotificationMessage>>(cacheKey);

            if (notifications != null)
            {
                var unreadCount = notifications.Count(n => !n.IsRead);
                var countCacheKey = $"{NotificationCountCachePrefix}{userId}";
                await _cacheService.SetAsync(countCacheKey, unreadCount, TimeSpan.FromMinutes(5));
            }
        }
    }
}
