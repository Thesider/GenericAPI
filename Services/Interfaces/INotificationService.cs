using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GenericAPI.Services
{
    /// <summary>
    /// Notification types
    /// </summary>
    public enum NotificationType
    {
        Info,
        Warning,
        Error,
        Success
    }

    /// <summary>
    /// Notification message model
    /// </summary>
    public class NotificationMessage
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string UserId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public NotificationType Type { get; set; } = NotificationType.Info;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsRead { get; set; } = false;
        public Dictionary<string, object> Data { get; set; } = new();
    }

    /// <summary>
    /// Interface for notification service operations
    /// </summary>
    public interface INotificationService
    {
        /// <summary>
        /// Sends a notification to a specific user
        /// </summary>
        /// <param name="userId">Target user ID</param>
        /// <param name="title">Notification title</param>
        /// <param name="message">Notification message</param>
        /// <param name="type">Notification type</param>
        /// <param name="data">Additional data</param>
        /// <returns>Notification ID</returns>
        Task<string> SendNotificationAsync(string userId, string title, string message, 
            NotificationType type = NotificationType.Info, Dictionary<string, object>? data = null);

        /// <summary>
        /// Sends a notification to multiple users
        /// </summary>
        /// <param name="userIds">Target user IDs</param>
        /// <param name="title">Notification title</param>
        /// <param name="message">Notification message</param>
        /// <param name="type">Notification type</param>
        /// <param name="data">Additional data</param>
        /// <returns>List of notification IDs</returns>
        Task<List<string>> SendBulkNotificationAsync(List<string> userIds, string title, string message,
            NotificationType type = NotificationType.Info, Dictionary<string, object>? data = null);

        /// <summary>
        /// Gets notifications for a user
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="unreadOnly">Get only unread notifications</param>
        /// <param name="limit">Maximum number of notifications</param>
        /// <returns>List of notifications</returns>
        Task<List<NotificationMessage>> GetUserNotificationsAsync(string userId, bool unreadOnly = false, int limit = 50);

        /// <summary>
        /// Marks a notification as read
        /// </summary>
        /// <param name="notificationId">Notification ID</param>
        /// <param name="userId">User ID</param>
        /// <returns>True if marked successfully</returns>
        Task<bool> MarkAsReadAsync(string notificationId, string userId);

        /// <summary>
        /// Marks all notifications as read for a user
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <returns>Number of notifications marked as read</returns>
        Task<int> MarkAllAsReadAsync(string userId);

        /// <summary>
        /// Deletes a notification
        /// </summary>
        /// <param name="notificationId">Notification ID</param>
        /// <param name="userId">User ID</param>
        /// <returns>True if deleted successfully</returns>
        Task<bool> DeleteNotificationAsync(string notificationId, string userId);

        /// <summary>
        /// Gets unread notification count for a user
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <returns>Number of unread notifications</returns>
        Task<int> GetUnreadCountAsync(string userId);

        /// <summary>
        /// Sends order status notification
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="orderId">Order ID</param>
        /// <param name="status">New order status</param>
        Task SendOrderStatusNotificationAsync(string userId, int orderId, string status);

        /// <summary>
        /// Sends low stock notification to admins
        /// </summary>
        /// <param name="productId">Product ID</param>
        /// <param name="productName">Product name</param>
        /// <param name="currentStock">Current stock level</param>
        Task SendLowStockNotificationAsync(int productId, string productName, int currentStock);
    }
}
