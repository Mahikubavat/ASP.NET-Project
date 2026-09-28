using BlogPlatform.Data;
using BlogPlatform.Models;
using Microsoft.EntityFrameworkCore;

namespace BlogPlatform.Services;

public class NotificationService : INotificationService
{
    private readonly ApplicationDbContext _db;

    public NotificationService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task CreateNotificationAsync(string recipientUserId, string? triggeredByUserId, string type, string message, string? url)
    {
        if (string.IsNullOrWhiteSpace(recipientUserId) || recipientUserId == triggeredByUserId)
        {
            return; // Don't notify self
        }

        var recipient = await _db.Users.FindAsync(recipientUserId);
        if (recipient == null) return;

        // Check user preferences:
        if (type.Equals("Like", StringComparison.OrdinalIgnoreCase) && !recipient.NotifyOnLike)
        {
            return;
        }

        if ((type.Equals("Comment", StringComparison.OrdinalIgnoreCase) || type.Equals("Reply", StringComparison.OrdinalIgnoreCase)) && !recipient.NotifyOnComment)
        {
            return;
        }

        var notification = new Notification
        {
            UserId = recipientUserId,
            TriggeredByUserId = triggeredByUserId,
            Type = type,
            Message = message,
            Url = url,
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        };

        _db.Notifications?.Add(notification);
        await _db.SaveChangesAsync();
    }

    public async Task<int> GetUnreadCountAsync(string userId)
    {
        if (_db.Notifications == null) return 0;
        return await _db.Notifications.CountAsync(n => n.UserId == userId && !n.IsRead);
    }

    public async Task<List<Notification>> GetUserNotificationsAsync(string userId, int count = 20)
    {
        if (_db.Notifications == null) return new List<Notification>();
        return await _db.Notifications
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .Take(count)
            .Include(n => n.TriggeredByUser)
            .ToListAsync();
    }

    public async Task<bool> MarkAsReadAsync(int notificationId, string userId)
    {
        if (_db.Notifications == null) return false;
        var notification = await _db.Notifications.FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId);
        if (notification == null) return false;

        notification.IsRead = true;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task MarkAllAsReadAsync(string userId)
    {
        if (_db.Notifications == null) return;
        var unread = await _db.Notifications.Where(n => n.UserId == userId && !n.IsRead).ToListAsync();
        foreach (var item in unread)
        {
            item.IsRead = true;
        }
        await _db.SaveChangesAsync();
    }
}
