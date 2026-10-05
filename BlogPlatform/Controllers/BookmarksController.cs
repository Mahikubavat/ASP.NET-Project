using BlogPlatform.Data;
using BlogPlatform.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace BlogPlatform.Controllers;

[Authorize]
public class BookmarksController : Controller
{
    private readonly ApplicationDbContext _db;

    public BookmarksController(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<IActionResult> Index()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId)) return Challenge();

        var bookmarks = await _db.Bookmarks!
            .Where(bookmark => bookmark.UserId == userId && bookmark.Post != null && bookmark.Post.Status == PostStatus.Published)
            .OrderByDescending(bookmark => bookmark.CreatedAt)
            .Include(bookmark => bookmark.Post)!
                .ThenInclude(post => post!.Author)
            .Include(bookmark => bookmark.Post)!
                .ThenInclude(post => post!.Category)
            .ToListAsync();

        return View(bookmarks);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Toggle(int postId)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId)) return Challenge();

        var postExists = await _db.Posts!
            .AnyAsync(post => post.Id == postId && post.Status == PostStatus.Published);
        if (!postExists) return NotFound();

        var bookmark = await _db.Bookmarks!
            .FirstOrDefaultAsync(item => item.PostId == postId && item.UserId == userId);

        if (bookmark == null)
        {
            _db.Bookmarks!.Add(new Bookmark { PostId = postId, UserId = userId });
            TempData["SuccessMessage"] = "Post saved to your bookmarks.";
        }
        else
        {
            _db.Bookmarks!.Remove(bookmark);
            TempData["SuccessMessage"] = "Post removed from your bookmarks.";
        }

        await _db.SaveChangesAsync();
        return RedirectToAction("Details", "Posts", new { id = postId });
    }
}