using BlogPlatform.Data;
using BlogPlatform.Models;
using BlogPlatform.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace BlogPlatform.Controllers;

[Authorize]
public class NotificationsController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly INotificationService _notificationService;
    private readonly UserManager<ApplicationUser> _userManager;

    public NotificationsController(
        ApplicationDbContext db,
        INotificationService notificationService,
        UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _notificationService = notificationService;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index(bool unreadOnly = false)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId == null) return Challenge();

        var query = _db.Notifications!
            .Where(n => n.UserId == userId);

        if (unreadOnly)
        {
            query = query.Where(n => !n.IsRead);
        }

        var notifications = await query
            .OrderByDescending(n => n.CreatedAt)
            .Take(50)
            .Include(n => n.TriggeredByUser)
            .ToListAsync();

        var user = await _userManager.FindByIdAsync(userId);
        ViewBag.NotifyOnLike = user?.NotifyOnLike ?? true;
        ViewBag.NotifyOnComment = user?.NotifyOnComment ?? true;
        ViewBag.UnreadOnly = unreadOnly;

        return View(notifications);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkAsRead(int id, string? returnUrl = null)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId == null) return Challenge();

        await _notificationService.MarkAsReadAsync(id, userId);

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkAllAsRead()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId == null) return Challenge();

        await _notificationService.MarkAllAsReadAsync(userId);
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> GetUnreadCount()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId == null) return Json(new { count = 0 });

        var count = await _notificationService.GetUnreadCountAsync(userId);
        return Json(new { count });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleSettings(bool notifyOnLike, bool notifyOnComment)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId == null) return Challenge();

        var user = await _userManager.FindByIdAsync(userId);
        if (user != null)
        {
            user.NotifyOnLike = notifyOnLike;
            user.NotifyOnComment = notifyOnComment;
            await _userManager.UpdateAsync(user);
            TempData["SuccessMessage"] = "Notification preferences updated successfully.";
        }

        return RedirectToAction(nameof(Index));
    }
}
