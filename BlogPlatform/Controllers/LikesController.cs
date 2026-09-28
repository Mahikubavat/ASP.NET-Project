using BlogPlatform.Data;
using BlogPlatform.Models;
using BlogPlatform.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace BlogPlatform.Controllers;

[Authorize]
public class LikesController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly INotificationService _notificationService;

    public LikesController(ApplicationDbContext db, INotificationService notificationService)
    {
        _db = db;
        _notificationService = notificationService;
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Toggle(int postId)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId == null) return Forbid();

        var post = await _db.Posts!.FindAsync(postId);
        if (post == null) return NotFound();

        var like = await _db.Likes!.FindAsync(postId, userId);
        bool isLikedNow = false;

        if (like == null)
        {
            _db.Likes.Add(new Like { PostId = postId, UserId = userId });
            isLikedNow = true;
        }
        else
        {
            _db.Likes.Remove(like);
        }

        await _db.SaveChangesAsync();

        if (isLikedNow && !string.IsNullOrEmpty(post.AuthorId) && post.AuthorId != userId)
        {
            var currentUserName = User.Identity?.Name ?? "Someone";
            string postUrl = Url.Action("Details", "Posts", new { id = postId }) + "#likes-section";
            await _notificationService.CreateNotificationAsync(
                post.AuthorId,
                userId,
                "Like",
                $"{currentUserName} liked your post \"{post.Title}\"",
                postUrl
            );
        }

        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
        {
            var likeCount = await _db.Likes!.CountAsync(l => l.PostId == postId);
            return Json(new { success = true, isLiked = isLikedNow, count = likeCount });
        }

        return RedirectToAction("Details", "Posts", new { id = postId });
    }
}
