using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopNongSan.Data;
using ShopNongSan.Models;
using System.Security.Claims;

namespace ShopNongSan.Controllers
{
    [Authorize] // bắt buộc đăng nhập mới xem được lịch sử
    public class OrdersController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;

        public OrdersController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        // GET: /Orders
        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User)!;

            var orders = await _db.Orders
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.CreatedAt)
                .Select(o => new
                {
                    o.Id,
                    o.CreatedAt,
                    o.Status,
                    o.Total,
                    ItemsCount = o.Items.Count
                })
                .ToListAsync();

            // có thể trả thẳng anonymous object qua ViewBag, hoặc map sang 1 VM nhanh gọn
            ViewBag.Orders = orders;
            return View();
        }

        // GET: /Orders/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var userId = _userManager.GetUserId(User)!;

            var order = await _db.Orders
                .Include(o => o.Items)
                    .ThenInclude(i => i.Product)
                .Include(o => o.User)
                .FirstOrDefaultAsync(o => o.Id == id && o.UserId == userId);

            if (order == null) return NotFound();

            return View(order);
        }

        // POST: /Orders/Cancel/5  (tuỳ chọn)
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            var userId = _userManager.GetUserId(User)!;

            var order = await _db.Orders
                .FirstOrDefaultAsync(o => o.Id == id && o.UserId == userId);

            if (order == null) return NotFound();

            if (order.Status == "Pending")
            {
                order.Status = "Canceled";
                await _db.SaveChangesAsync();
                TempData["msg"] = "Đã huỷ đơn hàng.";
            }
            else
            {
                TempData["msg"] = "Đơn hàng không thể huỷ.";
            }

            return RedirectToAction(nameof(Details), new { id });
        }
    }
}
