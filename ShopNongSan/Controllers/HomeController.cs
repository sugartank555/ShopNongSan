using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopNongSan.Data;
using ShopNongSan.ViewModels;

namespace ShopNongSan.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _db;
        public HomeController(ApplicationDbContext db) => _db = db;

        // GET /
        // GET /home
        public async Task<IActionResult> Index(string? q, int? categoryId)
        {
            var vm = new StoreHomeVM
            {
                Categories = await _db.Categories.OrderBy(c => c.Name).ToListAsync(),
                Q = q,
                CategoryId = categoryId
            };

            var query = _db.Products.AsNoTracking().OrderByDescending(p => p.Id).AsQueryable();
            if (!string.IsNullOrWhiteSpace(q)) query = query.Where(p => p.Name.Contains(q));
            if (categoryId.HasValue) query = query.Where(p => p.CategoryId == categoryId.Value);

            vm.Featured = await query.Take(12).ToListAsync();
            return View(vm); // Views/Home/Index.cshtml
        }

        // GET /product/slug/5
        [Route("product/{slug}/{id:int}")]
        public async Task<IActionResult> Product(string slug, int id)
        {
            var p = await _db.Products
                .Include(x => x.Category)
                .FirstOrDefaultAsync(x => x.Id == id);
            if (p == null) return NotFound();

            var related = await _db.Products
                .Where(x => x.CategoryId == p.CategoryId && x.Id != p.Id)
                .OrderByDescending(x => x.Id).Take(8).ToListAsync();

            return View(new ProductDetailVM { Product = p, Related = related });
            // View: Views/Home/Product.cshtml
        }
    }
}
