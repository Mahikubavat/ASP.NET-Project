using BlogPlatform.Data;
using BlogPlatform.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace BlogPlatform.Controllers;

[Authorize]
public class PostsController : Controller
{
    private readonly ApplicationDbContext _db;

    public PostsController(ApplicationDbContext db)
    {
        _db = db;
    }

    [AllowAnonymous]
    public async Task<IActionResult> Index(int page = 1, int pageSize = 10)
    {
        var query = _db.Posts!.AsQueryable();

        if (User?.Identity?.IsAuthenticated == true && User.IsInRole("Admin"))
        {
            // Admins manage every post, published or draft
        }
        else if (User?.Identity?.IsAuthenticated == true && User.IsInRole("Writer"))
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            // Writers see all published posts plus their own drafts
            query = query.Where(p => p.Status == PostStatus.Published || p.AuthorId == userId);
        }
        else
        {
            query = query.Where(p => p.Status == PostStatus.Published);
        }

        var posts = await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Include(p => p.Author)
            .Include(p => p.Category)
            .ToListAsync();

        return View(posts);
    }

    [AllowAnonymous]
    public async Task<IActionResult> Details(int id)
    {
        var post = await _db.Posts!
            .Include(p => p.Author)
            .Include(p => p.Category)
            .Include(p => p.PostTags)!.ThenInclude(pt => pt.Tag)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (post == null) return NotFound();

        if (post.Status != PostStatus.Published)
        {
            var userId = User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var canView = User?.Identity?.IsAuthenticated == true &&
                (User.IsInRole("Admin") || post.AuthorId == userId);
            if (!canView) return NotFound();
        }

        return View(post);
    }

    [Authorize(Roles = "Writer,Admin")]
    public async Task<IActionResult> Create()
    {
        await PopulateCategoriesAsync();
        return View(new Post());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Writer,Admin")]
    public async Task<IActionResult> Create(Post model)
    {
        if (!ModelState.IsValid)
        {
            await PopulateCategoriesAsync(model.CategoryId);
            return View(model);
        }

        model.AuthorId = User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        model.CreatedAt = DateTime.UtcNow;
        _db.Posts!.Add(model);
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "Writer,Admin")]
    public async Task<IActionResult> Edit(int id)
    {
        var post = await _db.Posts!.FindAsync(id);
        if (post == null) return NotFound();
        var userId = User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (User.IsInRole("Writer") && post.AuthorId != userId)
            return Forbid();
        await PopulateCategoriesAsync(post.CategoryId);
        return View(post);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Writer,Admin")]
    public async Task<IActionResult> Edit(int id, Post model)
    {
        if (id != model.Id) return BadRequest();
        if (!ModelState.IsValid)
        {
            await PopulateCategoriesAsync(model.CategoryId);
            return View(model);
        }

        var post = await _db.Posts!.FindAsync(id);
        if (post == null) return NotFound();

        var userId = User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (User.IsInRole("Writer") && post.AuthorId != userId)
            return Forbid();

        post.Title = model.Title;
        post.Content = model.Content;
        post.FeaturedImageUrl = model.FeaturedImageUrl;
        post.CategoryId = model.CategoryId;
        post.Status = model.Status;
        post.UpdatedAt = DateTime.UtcNow;

        _db.Update(post);
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "Writer,Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var post = await _db.Posts!.FindAsync(id);
        if (post == null) return NotFound();
        var userId = User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (User.IsInRole("Writer") && post.AuthorId != userId)
            return Forbid();

        return View(post);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Writer,Admin")]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var post = await _db.Posts!.FindAsync(id);
        if (post == null) return NotFound();
        var userId = User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (User.IsInRole("Writer") && post.AuthorId != userId)
            return Forbid();

        _db.Posts!.Remove(post);
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    private async Task PopulateCategoriesAsync(int? selectedId = null)
    {
        var categories = await _db.Categories!.OrderBy(c => c.Name).ToListAsync();
        ViewBag.Categories = new SelectList(categories, "Id", "Name", selectedId);
    }
}
