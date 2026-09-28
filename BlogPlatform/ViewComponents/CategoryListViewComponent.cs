using BlogPlatform.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BlogPlatform.ViewComponents
{
    public class CategoryListViewComponent : ViewComponent
    {
        private readonly ApplicationDbContext _db;
        public CategoryListViewComponent(ApplicationDbContext db) { _db = db; }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var categories = await _db.Categories!.OrderBy(c => c.Name).ToListAsync();
            return View(categories);
        }
    }
}
