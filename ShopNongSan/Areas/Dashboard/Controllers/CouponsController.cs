using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopNongSan.Data;
using ShopNongSan.Models;

namespace ShopNongSan.Areas.Dashboard.Controllers
{
    [Area("Dashboard")]
    [Authorize(Roles = "Admin,Staff")]
    public class CouponsController : Controller
    {
        private readonly ApplicationDbContext _db;
        public CouponsController(ApplicationDbContext db) => _db = db;

        // Danh sách mã giảm giá
        public async Task<IActionResult> Index()
        {
            var data = await _db.Coupons.OrderByDescending(c => c.Id).ToListAsync();
            return View(data);
        }

        // GET: Thêm mới
        public IActionResult Create()
        {
            return View(new Coupon { ExpiryDate = DateTime.Today.AddMonths(1) });
        }

        // POST: Thêm mới
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Coupon m)
        {
            if (!ModelState.IsValid) return View(m);
            _db.Coupons.Add(m);
            await _db.SaveChangesAsync();
            TempData["Success"] = "Đã thêm mã giảm giá thành công.";
            return RedirectToAction(nameof(Index));
        }

        // GET: Sửa
        public async Task<IActionResult> Edit(int id)
        {
            var m = await _db.Coupons.FindAsync(id);
            if (m == null) return NotFound();
            return View(m);
        }

        // POST: Sửa
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Coupon m)
        {
            if (!ModelState.IsValid) return View(m);
            _db.Update(m);
            await _db.SaveChangesAsync();
            TempData["Success"] = "Đã cập nhật thông tin mã giảm giá.";
            return RedirectToAction(nameof(Index));
        }

        // POST: Xóa
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var m = await _db.Coupons.FindAsync(id);
            if (m != null)
            {
                _db.Remove(m);
                await _db.SaveChangesAsync();
                TempData["Success"] = "Đã xóa mã giảm giá.";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
