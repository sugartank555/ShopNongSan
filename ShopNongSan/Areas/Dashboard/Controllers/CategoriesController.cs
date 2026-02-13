using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopNongSan.Data;
using ShopNongSan.Models;

namespace ShopNongSan.Areas.Dashboard.Controllers
{
    [Area("Dashboard")]
    [Authorize(Roles = "Admin,Staff")]
    public class CategoriesController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IWebHostEnvironment _env;

        public CategoriesController(ApplicationDbContext db, IWebHostEnvironment env)
        {
            _db = db;
            _env = env;
        }

        public async Task<IActionResult> Index()
        {
            var data = await _db.Categories.OrderBy(x => x.Name).ToListAsync();
            return View(data);
        }

        public IActionResult Create() => View(new Category());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Category m, IFormFile? imageFile)
        {
            if (!ModelState.IsValid) return View(m);

            if (imageFile != null && imageFile.Length > 0)
            {
                string uploadDir = Path.Combine(_env.WebRootPath, "uploads/categories");
                if (!Directory.Exists(uploadDir))
                    Directory.CreateDirectory(uploadDir);

                string fileName = Guid.NewGuid() + Path.GetExtension(imageFile.FileName);
                string path = Path.Combine(uploadDir, fileName);

                using (var stream = new FileStream(path, FileMode.Create))
                {
                    await imageFile.CopyToAsync(stream);
                }

                m.ImageUrl = "/uploads/categories/" + fileName;
            }

            _db.Add(m);
            await _db.SaveChangesAsync();
            TempData["Success"] = "Đã thêm danh mục mới.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var m = await _db.Categories.FindAsync(id);
            if (m == null) return NotFound();
            return View(m);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Category m, IFormFile? imageFile)
        {
            if (!ModelState.IsValid) return View(m);

            var dbItem = await _db.Categories.FindAsync(m.Id);
            if (dbItem == null) return NotFound();

            dbItem.Name = m.Name;
            dbItem.Description = m.Description;
            dbItem.IsActive = m.IsActive;

            if (imageFile != null && imageFile.Length > 0)
            {
                string uploadDir = Path.Combine(_env.WebRootPath, "uploads/categories");
                if (!Directory.Exists(uploadDir))
                    Directory.CreateDirectory(uploadDir);

                string fileName = Guid.NewGuid() + Path.GetExtension(imageFile.FileName);
                string path = Path.Combine(uploadDir, fileName);

                using (var stream = new FileStream(path, FileMode.Create))
                {
                    await imageFile.CopyToAsync(stream);
                }

                dbItem.ImageUrl = "/uploads/categories/" + fileName;
            }

            await _db.SaveChangesAsync();
            TempData["Success"] = "Đã cập nhật danh mục.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var m = await _db.Categories.FindAsync(id);
            if (m != null)
            {
                _db.Remove(m);
                await _db.SaveChangesAsync();
                TempData["Success"] = "Đã xóa danh mục.";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
