using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopNongSan.Data;

namespace ShopNongSan.Controllers
{
    public class ShopController : Controller
    {
        private readonly ApplicationDbContext _db;
        public ShopController(ApplicationDbContext db) => _db = db;

        public async Task<IActionResult> Index(int? categoryId, string? q)
        {
            var categories = await _db.Categories.Where(c => c.IsActive).ToListAsync();
            var query = _db.Products.Include(p => p.Category).Where(p => p.IsActive).AsQueryable();
            if (categoryId.HasValue) query = query.Where(p => p.CategoryId == categoryId);
            if (!string.IsNullOrWhiteSpace(q)) query = query.Where(p => p.Name.Contains(q));

            ViewBag.Categories = categories;
            ViewBag.Query = q;
            ViewBag.CategoryId = categoryId;

            return View(await query.OrderByDescending(p => p.Id).ToListAsync());
        }

        public async Task<IActionResult> Details(int id)
        {
            // Lấy sản phẩm + danh mục
            var product = await _db.Products
                .Include(p => p.Category)
                .FirstOrDefaultAsync(p => p.Id == id && p.IsActive);

            if (product == null)
                return NotFound();

            // Lấy thông tin lô sản xuất
            var lots = await _db.BatchLots
                .Where(b => b.ProductId == id)
                .OrderByDescending(b => b.HarvestDate)
                .ToListAsync();

            ViewBag.Lots = lots;

            // Lấy sản phẩm liên quan (cùng danh mục, không tính sản phẩm hiện tại)
            var related = await _db.Products
                .Where(p => p.CategoryId == product.CategoryId
                            && p.Id != id
                            && p.IsActive)
                .OrderByDescending(p => p.Id)
                .Take(4)
                .ToListAsync();

            ViewBag.Related = related;

            return View(product);
        }

    }
}
