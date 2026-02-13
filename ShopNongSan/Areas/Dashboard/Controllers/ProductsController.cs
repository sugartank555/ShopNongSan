using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopNongSan.Data;
using ShopNongSan.Models;

namespace ShopNongSan.Areas.Dashboard.Controllers
{
    [Area("Dashboard")]
    [Authorize(Roles = "Admin,Staff")]
    public class ProductsController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IWebHostEnvironment _env;

        public ProductsController(ApplicationDbContext db, IWebHostEnvironment env)
        {
            _db = db;
            _env = env;
        }

        public async Task<IActionResult> Index()
        {
            var data = await _db.Products
                .Include(x => x.Category)
                .OrderByDescending(x => x.Id)
                .ToListAsync();
            return View(data);
        }

        public async Task<IActionResult> Create()
        {
            ViewBag.Categories = await _db.Categories
                .Where(c => c.IsActive)
                .ToListAsync();
            return View(new Product());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Product m, IFormFile? imageFile)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Categories = await _db.Categories.Where(c => c.IsActive).ToListAsync();
                return View(m);
            }

            if (imageFile != null && imageFile.Length > 0)
            {
                string folder = Path.Combine(_env.WebRootPath, "uploads/products");
                if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

                string fileName = Guid.NewGuid() + Path.GetExtension(imageFile.FileName);
                string path = Path.Combine(folder, fileName);

                using var stream = new FileStream(path, FileMode.Create);
                await imageFile.CopyToAsync(stream);

                m.ImageUrl = "/uploads/products/" + fileName;
            }

            _db.Add(m);
            await _db.SaveChangesAsync();
            TempData["Success"] = "Đã thêm sản phẩm mới!";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var m = await _db.Products.FindAsync(id);
            if (m == null) return NotFound();

            ViewBag.Categories = await _db.Categories.ToListAsync();
            return View(m);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Product m, IFormFile? imageFile)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Categories = await _db.Categories.ToListAsync();
                return View(m);
            }

            var old = await _db.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == m.Id);
            if (old == null) return NotFound();

            if (imageFile != null && imageFile.Length > 0)
            {
                string folder = Path.Combine(_env.WebRootPath, "uploads/products");
                if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

                // Xóa ảnh cũ
                if (!string.IsNullOrEmpty(old.ImageUrl))
                {
                    var oldPath = Path.Combine(_env.WebRootPath, old.ImageUrl.TrimStart('/'));
                    if (System.IO.File.Exists(oldPath))
                        System.IO.File.Delete(oldPath);
                }

                string fileName = Guid.NewGuid() + Path.GetExtension(imageFile.FileName);
                string newPath = Path.Combine(folder, fileName);
                using var stream = new FileStream(newPath, FileMode.Create);
                await imageFile.CopyToAsync(stream);

                m.ImageUrl = "/uploads/products/" + fileName;
            }
            else
            {
                m.ImageUrl = old.ImageUrl;
            }

            _db.Update(m);
            await _db.SaveChangesAsync();
            TempData["Success"] = "Đã cập nhật sản phẩm!";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var m = await _db.Products.FindAsync(id);
            if (m != null)
            {
                if (!string.IsNullOrEmpty(m.ImageUrl))
                {
                    var path = Path.Combine(_env.WebRootPath, m.ImageUrl.TrimStart('/'));
                    if (System.IO.File.Exists(path))
                        System.IO.File.Delete(path);
                }

                _db.Remove(m);
                await _db.SaveChangesAsync();
                TempData["Success"] = "Đã xóa sản phẩm!";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
