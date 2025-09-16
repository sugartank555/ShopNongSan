using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ShopNongSan.Data;
using ShopNongSan.Models;

namespace ShopNongSan.Areas.Dashboard.Controllers
{
    [Area("Dashboard")]
    [Authorize(Roles = "Admin")]
    public class ProductsController : Controller
    {
        private readonly ApplicationDbContext _db;
        public ProductsController(ApplicationDbContext db) => _db = db;

        // ================== INDEX ==================
        public async Task<IActionResult> Index(string? q, int? categoryId)
        {
            ViewBag.Categories = new SelectList(
                await _db.Categories.OrderBy(c => c.Name).ToListAsync(),
                "Id", "Name", categoryId
            );

            var query = _db.Products.Include(p => p.Category).AsQueryable();
            if (categoryId.HasValue)
                query = query.Where(p => p.CategoryId == categoryId.Value);

            if (!string.IsNullOrWhiteSpace(q))
                query = query.Where(p => p.Name.Contains(q));

            return View(await query.OrderByDescending(p => p.Id).ToListAsync());
        }

        // ================== CREATE ==================
        public async Task<IActionResult> Create()
        {
            ViewBag.Categories = new SelectList(
                await _db.Categories.OrderBy(c => c.Name).ToListAsync(),
                "Id", "Name"
            );

            // Gợi ý giá trị mặc định
            return View(new Product { Stock = 10, Price = 10000, IsOrganic = true });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Product model)
        {
            if (!ModelState.IsValid)
            {
                // In toàn bộ lỗi để dễ debug
                var all = string.Join(" | ", ModelState
                    .Where(kv => kv.Value!.Errors.Count > 0)
                    .Select(kv => $"{kv.Key}: {string.Join(",", kv.Value!.Errors.Select(e => e.ErrorMessage))}"));

                // Bắn lỗi hiển thị lên ValidationSummary
                ModelState.AddModelError(string.Empty, "Lỗi nhập liệu: " + all);

                ViewBag.Categories = new SelectList(
                    await _db.Categories.OrderBy(c => c.Name).ToListAsync(),
                    "Id", "Name", model.CategoryId
                );

                return View(model);
            }

            try
            {
                _db.Products.Add(model);
                await _db.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, "Lỗi lưu CSDL: " + ex.Message);
                ViewBag.Categories = new SelectList(
                    await _db.Categories.OrderBy(c => c.Name).ToListAsync(),
                    "Id", "Name", model.CategoryId
                );
                return View(model);
            }
        }

        // ================== EDIT ==================
        public async Task<IActionResult> Edit(int id)
        {
            var p = await _db.Products.FindAsync(id);
            if (p == null) return NotFound();

            ViewBag.Categories = new SelectList(
                await _db.Categories.OrderBy(c => c.Name).ToListAsync(),
                "Id", "Name", p.CategoryId
            );

            return View(p);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Product model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Categories = new SelectList(
                    await _db.Categories.OrderBy(c => c.Name).ToListAsync(),
                    "Id", "Name", model.CategoryId
                );
                return View(model);
            }

            try
            {
                _db.Update(model);
                await _db.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, "Lỗi cập nhật: " + ex.Message);
                ViewBag.Categories = new SelectList(
                    await _db.Categories.OrderBy(c => c.Name).ToListAsync(),
                    "Id", "Name", model.CategoryId
                );
                return View(model);
            }
        }

        // ================== DELETE ==================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var p = await _db.Products.FindAsync(id);
            if (p != null)
            {
                _db.Products.Remove(p);
                await _db.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
