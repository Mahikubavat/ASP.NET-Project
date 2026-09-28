using BlogPlatform.Data;
using BlogPlatform.Models;
using BlogPlatform.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace BlogPlatform.Controllers;

[Authorize]
public class CommentsController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly INotificationService _notificationService;

    public CommentsController(ApplicationDbContext db, INotificationService notificationService)
    {
        _db = db;
        _notificationService = notificationService;
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(int postId, string content, int? parentCommentId = null)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return RedirectToAction("Details", "Posts", new { id = postId });
        }

        var post = await _db.Posts!.Include(p => p.Author).FirstOrDefaultAsync(p => p.Id == postId);
        if (post == null) return NotFound();

        var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var currentUserName = User.Identity?.Name ?? "Someone";

        // Validate parent comment if provided
        Comment? parentComment = null;
        if (parentCommentId.HasValue)
        {
            parentComment = await _db.Comments!
                .Include(c => c.Author)
                .FirstOrDefaultAsync(c => c.Id == parentCommentId.Value && c.PostId == postId);

            if (parentComment == null)
            {
                // Invalid parent comment ID for this post
                parentCommentId = null;
            }
        }

        var comment = new Comment
        {
            PostId = postId,
            Content = content.Trim(),
            AuthorId = currentUserId,
            ParentCommentId = parentCommentId,
            CreatedAt = DateTime.UtcNow,
            IsApproved = true
        };

        _db.Comments!.Add(comment);
        await _db.SaveChangesAsync();

        // Send notifications based on preferences
        string postUrl = Url.Action("Details", "Posts", new { id = postId }) + $"#comment-{comment.Id}";

        if (parentComment != null)
        {
            // Reply: Notify the author of the parent comment
            if (!string.IsNullOrEmpty(parentComment.AuthorId) && parentComment.AuthorId != currentUserId)
            {
                string msg = $"{currentUserName} replied to your comment on \"{post.Title}\"";
                await _notificationService.CreateNotificationAsync(
                    parentComment.AuthorId,
                    currentUserId,
                    "Reply",
                    msg,
                    postUrl
                );
            }
        }
        else
        {
            // Top-level comment: Notify the post author
            if (!string.IsNullOrEmpty(post.AuthorId) && post.AuthorId != currentUserId)
            {
                string msg = $"{currentUserName} commented on your post \"{post.Title}\"";
                await _notificationService.CreateNotificationAsync(
                    post.AuthorId,
                    currentUserId,
                    "Comment",
                    msg,
                    postUrl
                );
            }
        }

        return RedirectToAction("Details", "Posts", new { id = postId, fragment = $"comment-{comment.Id}" });
    }
}
