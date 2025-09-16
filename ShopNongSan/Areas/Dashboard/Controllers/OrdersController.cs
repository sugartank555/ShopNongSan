using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopNongSan.Data;

namespace ShopNongSan.Areas.Dashboard.Controllers
{
    [Area("Dashboard")]
    [Authorize(Roles = "Admin")]
    public class OrdersController : Controller
    {
        private readonly ApplicationDbContext _db;
        public OrdersController(ApplicationDbContext db) => _db = db;

        public async Task<IActionResult> Index(string? status)
        {
            var q = _db.Orders.AsQueryable();
            if (!string.IsNullOrWhiteSpace(status)) q = q.Where(o => o.Status == status);
            ViewBag.Status = status;
            return View(await q.OrderByDescending(o => o.CreatedAt).ToListAsync());
        }

        public async Task<IActionResult> Detail(int id)
        {
            var order = await _db.Orders
                .Include(o => o.Items).ThenInclude(i => i.Product)
                .Include(o => o.User)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null) return NotFound();
            return View(order);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int id, string status)
        {
            var order = await _db.Orders.FindAsync(id);
            if (order == null) return NotFound();

            order.Status = status; // Pending | Paid | Shipped | Completed | Canceled
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Detail), new { id });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var order = await _db.Orders.FindAsync(id);
            if (order != null)
            {
                var items = _db.OrderItems.Where(i => i.OrderId == order.Id);
                _db.OrderItems.RemoveRange(items);
                _db.Orders.Remove(order);
                await _db.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
