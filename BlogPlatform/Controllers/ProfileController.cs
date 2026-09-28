using BlogPlatform.Data;
using BlogPlatform.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace BlogPlatform.Controllers;

[Authorize]
public class ProfileController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly BlogPlatform.Services.IFileStorageService _fileStorage;

    public ProfileController(ApplicationDbContext db, BlogPlatform.Services.IFileStorageService fileStorage)
    {
        _db = db;
        _fileStorage = fileStorage;
    }

    [AllowAnonymous]
    public IActionResult Index()
    {
        return RedirectToAction(nameof(Public));
    }

    [AllowAnonymous]
    public async Task<IActionResult> Public(string? id)
    {
        // If id is not specified, fall back to current logged-in user
        if (string.IsNullOrEmpty(id))
        {
            id = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        }

        if (string.IsNullOrEmpty(id))
        {
            return RedirectToAction("Index", "Home");
        }

        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Id == id || u.UserName == id);

        if (user == null) return NotFound();

        // Safely load published posts for this user without invalid EF Core ternary include expressions
        user.Posts = await _db.Posts!
            .Where(p => p.AuthorId == user.Id && p.Status == Models.PostStatus.Published)
            .OrderByDescending(p => p.CreatedAt)
            .Include(p => p.Category)
            .Include(p => p.PostTags)!.ThenInclude(pt => pt.Tag)
            .Include(p => p.Likes)
            .Include(p => p.Comments)
            .ToListAsync();

        return View(user);
    }

    public async Task<IActionResult> Edit()
    {
        var uid = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (uid == null) return Forbid();
        var user = await _db.Users.FindAsync(uid);
        if (user == null) return NotFound();

        var vm = new ProfileEditViewModel
        {
            Bio = user.Bio,
            ProfilePictureUrl = user.ProfilePictureUrl ?? user.AvatarUrl,
            TwitterUrl = user.TwitterUrl,
            LinkedInUrl = user.LinkedInUrl,
            GitHubUrl = user.GitHubUrl,
            NotifyOnLike = user.NotifyOnLike,
            NotifyOnComment = user.NotifyOnComment
        };
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(ProfileEditViewModel vm)
    {
        var uid = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (uid == null) return Forbid();
        var user = await _db.Users.FindAsync(uid);
        if (user == null) return NotFound();

        if (!ModelState.IsValid) return View(vm);

        user.Bio = vm.Bio;
        user.TwitterUrl = vm.TwitterUrl;
        user.LinkedInUrl = vm.LinkedInUrl;
        user.GitHubUrl = vm.GitHubUrl;
        user.NotifyOnLike = vm.NotifyOnLike;
        user.NotifyOnComment = vm.NotifyOnComment;

        if (vm.ProfilePicture != null)
        {
            user.ProfilePictureUrl = await _fileStorage.SaveFileAsync(vm.ProfilePicture, "profiles");
        }

        _db.Update(user);
        await _db.SaveChangesAsync();
        TempData["SuccessMessage"] = "Profile and notification preferences updated successfully!";
        return RedirectToAction(nameof(Public), new { id = user.Id });
    }
}
