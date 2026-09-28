using BlogPlatform.Models;

namespace BlogPlatform.Services;

public interface INotificationService
{
    Task CreateNotificationAsync(string recipientUserId, string? triggeredByUserId, string type, string message, string? url);
    Task<int> GetUnreadCountAsync(string userId);
    Task<List<Notification>> GetUserNotificationsAsync(string userId, int count = 20);
    Task<bool> MarkAsReadAsync(int notificationId, string userId);
    Task MarkAllAsReadAsync(string userId);
}
