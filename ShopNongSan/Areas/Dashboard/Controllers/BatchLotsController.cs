using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopNongSan.Data;
using ShopNongSan.Models;

namespace ShopNongSan.Areas.Dashboard.Controllers
{
    [Area("Dashboard")]
    [Authorize(Roles = "Admin,Staff")]
    public class BatchLotsController : Controller
    {
        private readonly ApplicationDbContext _db;
        public BatchLotsController(ApplicationDbContext db) => _db = db;

        // Danh sách lô hàng
        public async Task<IActionResult> Index()
        {
            var data = await _db.BatchLots
                .Include(b => b.Product)
                .OrderByDescending(b => b.HarvestDate)
                .ToListAsync();
            return View(data);
        }

        // Tạo mới
        public async Task<IActionResult> Create()
        {
            ViewBag.Products = await _db.Products
                .Where(p => p.IsActive)
                .OrderBy(p => p.Name)
                .ToListAsync();

            return View(new BatchLot
            {
                HarvestDate = DateTime.Today,
                ExpireDate = DateTime.Today.AddMonths(3)
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(BatchLot m)
        {
            if (m.ExpireDate.HasValue && m.ExpireDate <= m.HarvestDate)
                ModelState.AddModelError("ExpireDate", "Ngày hết hạn phải sau ngày thu hoạch.");

            if (!ModelState.IsValid)
            {
                ViewBag.Products = await _db.Products.Where(p => p.IsActive).ToListAsync();
                return View(m);
            }

            _db.BatchLots.Add(m);
            await _db.SaveChangesAsync();
            TempData["Success"] = "Đã thêm lô hàng mới!";
            return RedirectToAction(nameof(Index));
        }

        // Sửa
        public async Task<IActionResult> Edit(int id)
        {
            var lot = await _db.BatchLots.FindAsync(id);
            if (lot == null) return NotFound();

            ViewBag.Products = await _db.Products.ToListAsync();
            return View(lot);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(BatchLot m)
        {
            if (m.ExpireDate.HasValue && m.ExpireDate <= m.HarvestDate)
                ModelState.AddModelError("ExpireDate", "Ngày hết hạn phải sau ngày thu hoạch.");

            if (!ModelState.IsValid)
            {
                ViewBag.Products = await _db.Products.ToListAsync();
                return View(m);
            }

            _db.Update(m);
            await _db.SaveChangesAsync();
            TempData["Success"] = "Đã cập nhật lô hàng!";
            return RedirectToAction(nameof(Index));
        }

        // Xóa
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var m = await _db.BatchLots.FindAsync(id);
            if (m != null)
            {
                _db.BatchLots.Remove(m);
                await _db.SaveChangesAsync();
                TempData["Success"] = "Đã xóa lô hàng!";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
