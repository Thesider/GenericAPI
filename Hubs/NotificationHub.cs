using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;
using GenericAPI.Services.Interfaces;
using System.Security.Claims;

namespace GenericAPI.Hubs;

[Authorize]
public class NotificationHub : Hub
{
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<NotificationHub> _logger;

    public NotificationHub(IAuditLogService auditLogService, ILogger<NotificationHub> logger)
    {
        _auditLogService = auditLogService;
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        var userId = Context.UserIdentifier;
        var connectionId = Context.ConnectionId;
        
        _logger.LogInformation("User {UserId} connected with connection {ConnectionId}", userId, connectionId);
        
        if (userId != null)
        {
            // Add user to their personal group for targeted notifications
            await Groups.AddToGroupAsync(connectionId, $"User_{userId}");
            
            // Log the connection event
            await _auditLogService.LogUserActionAsync(
                userId, 
                "SignalR_Connected", 
                $"Connection ID: {connectionId}",
                GetClientIpAddress()
            );
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = Context.UserIdentifier;
        var connectionId = Context.ConnectionId;
        
        _logger.LogInformation("User {UserId} disconnected from connection {ConnectionId}", userId, connectionId);
        
        if (userId != null)
        {
            // Remove user from their personal group
            await Groups.RemoveFromGroupAsync(connectionId, $"User_{userId}");
            
            // Log the disconnection event
            await _auditLogService.LogUserActionAsync(
                userId, 
                "SignalR_Disconnected", 
                $"Connection ID: {connectionId}. Exception: {exception?.Message}",
                GetClientIpAddress()
            );
        }

        await base.OnDisconnectedAsync(exception);
    }

    // Method for clients to join specific groups (e.g., order updates, product notifications)
    public async Task JoinGroup(string groupName)
    {
        var userId = Context.UserIdentifier;
        var connectionId = Context.ConnectionId;
        
        // Validate group name to prevent abuse
        if (IsValidGroupName(groupName) && userId != null)
        {
            await Groups.AddToGroupAsync(connectionId, groupName);
            
            _logger.LogInformation("User {UserId} joined group {GroupName}", userId, groupName);
            
            await _auditLogService.LogUserActionAsync(
                userId, 
                "SignalR_JoinGroup", 
                $"Group: {groupName}",
                GetClientIpAddress()
            );
            
            await Clients.Caller.SendAsync("JoinedGroup", groupName);
        }
        else
        {
            await Clients.Caller.SendAsync("Error", "Invalid group name or unauthorized");
        }
    }

    // Method for clients to leave specific groups
    public async Task LeaveGroup(string groupName)
    {
        var userId = Context.UserIdentifier;
        var connectionId = Context.ConnectionId;
        
        if (IsValidGroupName(groupName) && userId != null)
        {
            await Groups.RemoveFromGroupAsync(connectionId, groupName);
            
            _logger.LogInformation("User {UserId} left group {GroupName}", userId, groupName);
            
            await _auditLogService.LogUserActionAsync(
                userId, 
                "SignalR_LeaveGroup", 
                $"Group: {groupName}",
                GetClientIpAddress()
            );
            
            await Clients.Caller.SendAsync("LeftGroup", groupName);
        }
    }

    // Method for clients to send messages to groups (with proper authorization)
    public async Task SendToGroup(string groupName, string message)
    {
        var userId = Context.UserIdentifier;
        
        if (IsValidGroupName(groupName) && userId != null && !string.IsNullOrWhiteSpace(message))
        {
            // Check if user has permission to send to this group
            if (await CanSendToGroup(userId, groupName))
            {
                await Clients.Group(groupName).SendAsync("ReceiveMessage", userId, message, DateTime.UtcNow);
                
                await _auditLogService.LogUserActionAsync(
                    userId, 
                    "SignalR_SendMessage", 
                    $"Group: {groupName}, Message: {message[..Math.Min(message.Length, 100)]}",
                    GetClientIpAddress()
                );
            }
            else
            {
                await Clients.Caller.SendAsync("Error", "Unauthorized to send to this group");
            }
        }
    }

    private bool IsValidGroupName(string groupName)
    {
        // Define valid group patterns
        var validPatterns = new[]
        {
            "Orders_",
            "Products_",
            "Notifications_",
            "Admin_",
            "Support_"
        };

        return validPatterns.Any(pattern => groupName.StartsWith(pattern)) && 
               groupName.Length <= 50 && 
               groupName.All(c => char.IsLetterOrDigit(c) || c == '_');
    }

    private async Task<bool> CanSendToGroup(string userId, string groupName)
    {
        // Implement authorization logic based on user roles and group permissions
        var userRole = Context.User?.FindFirst(ClaimTypes.Role)?.Value;
        
        // Admin can send to any group
        if (userRole == "Admin")
            return true;

        // Users can only send to certain groups
        if (groupName.StartsWith("Support_") || groupName.StartsWith("Orders_"))
            return true;

        // Add more specific authorization logic as needed
        await Task.CompletedTask;
        return false;
    }

    private string? GetClientIpAddress()
    {
        return Context.GetHttpContext()?.Connection?.RemoteIpAddress?.ToString();
    }
}

// Extension methods for easier notification sending from services
public static class NotificationHubExtensions
{
    public static async Task SendNotificationToUser(this IHubContext<NotificationHub> hubContext, 
        string userId, string type, object data)
    {
        await hubContext.Clients.Group($"User_{userId}")
            .SendAsync("Notification", new
            {
                Type = type,
                Data = data,
                Timestamp = DateTime.UtcNow
            });
    }

    public static async Task SendNotificationToGroup(this IHubContext<NotificationHub> hubContext, 
        string groupName, string type, object data)
    {
        await hubContext.Clients.Group(groupName)
            .SendAsync("Notification", new
            {
                Type = type,
                Data = data,
                Timestamp = DateTime.UtcNow
            });
    }

    public static async Task SendSystemAlert(this IHubContext<NotificationHub> hubContext, 
        string message, string severity = "info")
    {
        await hubContext.Clients.All
            .SendAsync("SystemAlert", new
            {
                Message = message,
                Severity = severity,
                Timestamp = DateTime.UtcNow
            });
    }
}
