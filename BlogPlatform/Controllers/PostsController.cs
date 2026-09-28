using BlogPlatform.Data;
using BlogPlatform.Models;
using BlogPlatform.Services;
using BlogPlatform.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace BlogPlatform.Controllers;

[Authorize]
public class PostsController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly IFileStorageService _fileStorage;

    public PostsController(ApplicationDbContext db, IFileStorageService fileStorage)
    {
        _db = db;
        _fileStorage = fileStorage;
    }

    [AllowAnonymous]
    public async Task<IActionResult> Index(int? categoryId, string? q, string? tag, int page = 1, int pageSize = 12)
    {
        // Public explore shows all published articles
        var query = _db.Posts!.Where(p => p.Status == PostStatus.Published);

        if (categoryId.HasValue) query = query.Where(p => p.CategoryId == categoryId.Value);
        if (!string.IsNullOrWhiteSpace(q)) query = query.Where(p => p.Title.Contains(q) || p.Content.Contains(q) || (p.PostTags != null && p.PostTags.Any(pt => pt.Tag != null && pt.Tag.Name.Contains(q))));
        if (!string.IsNullOrWhiteSpace(tag)) query = query.Where(p => p.PostTags != null && p.PostTags.Any(pt => pt.Tag != null && pt.Tag.Name == tag));

        var posts = await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Include(p => p.Author)
            .Include(p => p.Category)
            .Include(p => p.PostTags)!.ThenInclude(pt => pt.Tag)
            .Include(p => p.Likes)
            .Include(p => p.Comments)
            .ToListAsync();

        ViewBag.CurrentCategory = categoryId;
        ViewBag.CurrentSearch = q;
        ViewBag.CurrentTag = tag;

        return View(posts);
    }

    [Authorize(Roles = "Writer,Admin")]
    public async Task<IActionResult> Manage(string? status, string? q)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId)) return Challenge();

        // Shows ONLY user's own posts (both published and drafts)
        var query = _db.Posts!.Where(p => p.AuthorId == userId);

        if (!string.IsNullOrWhiteSpace(status))
        {
            if (status.Equals("published", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(p => p.Status == PostStatus.Published);
            }
            else if (status.Equals("draft", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(p => p.Status == PostStatus.Draft);
            }
        }

        if (!string.IsNullOrWhiteSpace(q))
        {
            query = query.Where(p => p.Title.Contains(q) || p.Content.Contains(q));
        }

        var posts = await query
            .OrderByDescending(p => p.CreatedAt)
            .Include(p => p.Category)
            .Include(p => p.PostTags)!.ThenInclude(pt => pt.Tag)
            .ToListAsync();

        ViewBag.TotalCount = await _db.Posts!.CountAsync(p => p.AuthorId == userId);
        ViewBag.PublishedCount = await _db.Posts!.CountAsync(p => p.AuthorId == userId && p.Status == PostStatus.Published);
        ViewBag.DraftCount = await _db.Posts!.CountAsync(p => p.AuthorId == userId && p.Status == PostStatus.Draft);
        ViewBag.CurrentStatus = string.IsNullOrWhiteSpace(status) ? "all" : status.ToLowerInvariant();
        ViewBag.CurrentSearch = q;

        return View(posts);
    }

    [AllowAnonymous]
    public async Task<IActionResult> Details(int id)
    {
        var post = await _db.Posts!
            .Include(p => p.Author)
            .Include(p => p.Category)
            .Include(p => p.PostTags)!.ThenInclude(pt => pt.Tag)
            .Include(p => p.Comments)!.ThenInclude(c => c.Author)
            .Include(p => p.Comments)!.ThenInclude(c => c.Replies)!.ThenInclude(r => r.Author)
            .Include(p => p.Likes)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (post == null) return NotFound();

        if (post.Status != PostStatus.Published)
        {
            var userId = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var canView = User?.Identity?.IsAuthenticated == true &&
                (User.IsInRole("Admin") || post.AuthorId == userId);
            if (!canView) return NotFound();
        }

        return View(post);
    }

    [Authorize(Roles = "Writer,Admin")]
    public async Task<IActionResult> Create()
    {
        await PopulateViewBagsAsync();
        return View(new PostCreateEditViewModel { IsPublished = true });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Writer,Admin")]
    public async Task<IActionResult> Create(PostCreateEditViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await PopulateViewBagsAsync(model.CategoryId);
            return View(model);
        }

        var post = new Post
        {
            Title = model.Title,
            Slug = string.IsNullOrWhiteSpace(model.Slug) ? model.Title.ToLowerInvariant().Replace(' ', '-') : model.Slug,
            Content = model.Content,
            CategoryId = model.CategoryId,
            Status = model.IsPublished ? PostStatus.Published : PostStatus.Draft,
            CreatedAt = DateTime.UtcNow,
            AuthorId = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
        };

        if (model.FeaturedImage != null)
        {
            post.FeaturedImageUrl = await _fileStorage.SaveFileAsync(model.FeaturedImage, "posts");
        }

        _db.Posts!.Add(post);
        await _db.SaveChangesAsync();

        // Process both selected tags and custom entered tags
        await ProcessTagsAsync(post, model.SelectedTagIds, model.TagsCommaSeparated);
        await _db.SaveChangesAsync();

        TempData["SuccessMessage"] = post.Status == PostStatus.Published 
            ? "Post published successfully!" 
            : "Post saved as draft!";
        return RedirectToAction(nameof(Manage));
    }

    [Authorize(Roles = "Writer,Admin")]
    public async Task<IActionResult> Edit(int id)
    {
        var post = await _db.Posts!
            .Include(p => p.PostTags)!.ThenInclude(pt => pt.Tag)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (post == null) return NotFound();
        var userId = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (User.IsInRole("Writer") && post.AuthorId != userId)
            return Forbid();

        var vm = new PostCreateEditViewModel
        {
            Id = post.Id,
            Title = post.Title,
            Slug = post.Slug,
            Content = post.Content,
            CategoryId = post.CategoryId,
            IsPublished = post.Status == PostStatus.Published,
            ExistingFeaturedImageUrl = post.FeaturedImageUrl,
            SelectedTagIds = post.PostTags?.Select(pt => pt.TagId).ToList() ?? new List<int>()
        };

        await PopulateViewBagsAsync(post.CategoryId);
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Writer,Admin")]
    public async Task<IActionResult> Edit(int id, PostCreateEditViewModel model)
    {
        if (id != model.Id) return BadRequest();

        if (!ModelState.IsValid)
        {
            await PopulateViewBagsAsync(model.CategoryId);
            return View(model);
        }

        var post = await _db.Posts!
            .Include(p => p.PostTags)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (post == null) return NotFound();

        var userId = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (User.IsInRole("Writer") && post.AuthorId != userId)
            return Forbid();

        post.Title = model.Title;
        post.Slug = string.IsNullOrWhiteSpace(model.Slug) ? model.Title.ToLowerInvariant().Replace(' ', '-') : model.Slug;
        post.Content = model.Content;
        post.CategoryId = model.CategoryId;
        post.Status = model.IsPublished ? PostStatus.Published : PostStatus.Draft;
        post.UpdatedAt = DateTime.UtcNow;

        if (model.FeaturedImage != null)
        {
            if (!string.IsNullOrWhiteSpace(post.FeaturedImageUrl))
            {
                _fileStorage.DeleteFileIfExists(post.FeaturedImageUrl);
            }
            post.FeaturedImageUrl = await _fileStorage.SaveFileAsync(model.FeaturedImage, "posts");
        }

        // Clear existing post tags and re-process
        if (post.PostTags != null)
        {
            _db.PostTags!.RemoveRange(post.PostTags);
            post.PostTags.Clear();
        }

        await ProcessTagsAsync(post, model.SelectedTagIds, model.TagsCommaSeparated);

        _db.Update(post);
        await _db.SaveChangesAsync();

        TempData["SuccessMessage"] = "Post updated successfully!";
        return RedirectToAction(nameof(Manage));
    }

    [Authorize(Roles = "Writer,Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var post = await _db.Posts!
            .Include(p => p.Category)
            .Include(p => p.Author)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (post == null) return NotFound();
        var userId = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
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
        var userId = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (User.IsInRole("Writer") && post.AuthorId != userId)
            return Forbid();

        if (!string.IsNullOrEmpty(post.FeaturedImageUrl))
        {
            _fileStorage.DeleteFileIfExists(post.FeaturedImageUrl);
        }

        _db.Posts!.Remove(post);
        await _db.SaveChangesAsync();

        TempData["SuccessMessage"] = "Post deleted successfully!";
        return RedirectToAction(nameof(Manage));
    }

    private async Task PopulateViewBagsAsync(int? selectedCategoryId = null)
    {
        var categories = await _db.Categories!.OrderBy(c => c.Name).ToListAsync();
        ViewBag.Categories = new SelectList(categories, "Id", "Name", selectedCategoryId);
        ViewBag.CategoryList = categories;

        var tags = await _db.Tags!.OrderBy(t => t.Name).ToListAsync();
        ViewBag.Tags = tags;
    }

    private async Task ProcessTagsAsync(Post post, List<int>? selectedTagIds, string? tagsCommaSeparated)
    {
        post.PostTags ??= new List<PostTag>();
        var addedTagIds = new HashSet<int>();

        // 1. Add explicitly selected tags
        if (selectedTagIds != null && selectedTagIds.Any())
        {
            foreach (var tagId in selectedTagIds)
            {
                if (tagId > 0 && !addedTagIds.Contains(tagId))
                {
                    post.PostTags.Add(new PostTag { PostId = post.Id, TagId = tagId });
                    addedTagIds.Add(tagId);
                }
            }
        }

        // 2. Add custom tags entered via comma-separated input
        if (!string.IsNullOrWhiteSpace(tagsCommaSeparated))
        {
            var tagNames = tagsCommaSeparated
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(t => t.Trim())
                .Where(t => !string.IsNullOrEmpty(t))
                .Distinct(StringComparer.OrdinalIgnoreCase);

            foreach (var tagName in tagNames)
            {
                var existingTag = await _db.Tags!.FirstOrDefaultAsync(t => t.Name.ToLower() == tagName.ToLower());
                if (existingTag == null)
                {
                    existingTag = new Tag
                    {
                        Name = tagName,
                        Slug = tagName.ToLowerInvariant().Replace(' ', '-')
                    };
                    _db.Tags!.Add(existingTag);
                    await _db.SaveChangesAsync();
                }

                if (!addedTagIds.Contains(existingTag.Id))
                {
                    post.PostTags.Add(new PostTag { PostId = post.Id, TagId = existingTag.Id });
                    addedTagIds.Add(existingTag.Id);
                }
            }
        }

        // 3. Fallback to default "General" tag if post has zero tags
        if (!post.PostTags.Any())
        {
            var defaultTag = await _db.Tags!.FirstOrDefaultAsync(t => t.Name == "General");
            if (defaultTag == null)
            {
                defaultTag = new Tag { Name = "General", Slug = "general" };
                _db.Tags!.Add(defaultTag);
                await _db.SaveChangesAsync();
            }
            post.PostTags.Add(new PostTag { PostId = post.Id, TagId = defaultTag.Id });
        }
    }
}
