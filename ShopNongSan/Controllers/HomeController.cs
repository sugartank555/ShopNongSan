using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopNongSan.Data;

namespace ShopNongSan.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _db;

        public HomeController(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<IActionResult> Index()
        {
            // Danh mục
            var categories = await _db.Categories
                .Where(c => c.IsActive)
                .OrderBy(c => c.Name)
                .ToListAsync();

            ViewBag.Categories = categories;

            // Sản phẩm nổi bật
            var featuredProducts = await _db.Products
                .Where(p => p.IsActive && p.IsFeatured)
                .OrderByDescending(p => p.Id)
                .Take(8)
                .ToListAsync();

            ViewBag.FeaturedProducts = featuredProducts;

            // Sản phẩm mới nhất
            var newProducts = await _db.Products
                .Where(p => p.IsActive)
                .OrderByDescending(p => p.CreatedAt)
                .Take(8)
                .ToListAsync();

            return View(newProducts);
        }
    }
}
