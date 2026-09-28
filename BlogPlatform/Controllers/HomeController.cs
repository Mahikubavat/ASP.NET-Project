using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using BlogPlatform.Models;
using BlogPlatform.Data;
using Microsoft.EntityFrameworkCore;

namespace BlogPlatform.Controllers;

public class HomeController : Controller
{
    private readonly ApplicationDbContext _db;

    public HomeController(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<IActionResult> Index()
    {
        // Show latest published posts on the home page
        var posts = await _db.Posts!
            .Where(p => p.Status == PostStatus.Published)
            .OrderByDescending(p => p.CreatedAt)
            .Take(6)
            .Include(p => p.Author)
            .Include(p => p.Category)
            .Include(p => p.PostTags)!.ThenInclude(pt => pt.Tag)
            .Include(p => p.Likes)
            .Include(p => p.Comments)
            .ToListAsync();

        ViewBag.Categories = await _db.Categories!.OrderBy(c => c.Name).Take(8).ToListAsync();
        ViewBag.PopularTags = await _db.Tags!.OrderBy(t => t.Name).Take(10).ToListAsync();

        return View(posts);
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
