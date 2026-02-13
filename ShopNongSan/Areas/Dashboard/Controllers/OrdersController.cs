using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopNongSan.Data;
using ShopNongSan.Models;

namespace ShopNongSan.Areas.Dashboard.Controllers
{
    [Area("Dashboard")]
    [Authorize(Roles = "Admin,Staff")]
    public class OrdersController : Controller
    {
        private readonly ApplicationDbContext _db;
        public OrdersController(ApplicationDbContext db) => _db = db;


        // -------------------------------
        // 📦 Danh sách đơn + Lọc trạng thái
        // -------------------------------
        public async Task<IActionResult> Index(OrderStatus? status)
        {
            var query = _db.Orders.Include(o => o.Items).AsQueryable();

            if (status.HasValue)
                query = query.Where(o => o.Status == status);

            var data = await query.OrderByDescending(o => o.CreatedAt).ToListAsync();

            ViewBag.CurrentStatus = status;
            return View(data);
        }


        // -------------------------------
        // 🔍 Chi tiết đơn hàng
        // -------------------------------
        public async Task<IActionResult> Details(int id)
        {
            var order = await _db.Orders
                .Include(o => o.Items)
                .ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null) return NotFound();

            order.Total = order.Items.Sum(i => i.UnitPrice * i.Quantity) - order.DiscountAmount;

            return View(order);
        }


        // -------------------------------
        // 🔄 Cập nhật trạng thái
        // -------------------------------
        [HttpPost]
        public async Task<IActionResult> UpdateStatus(int id, OrderStatus status)
        {
            var order = await _db.Orders.FindAsync(id);
            if (order == null) return NotFound();

            order.Status = status;
            await _db.SaveChangesAsync();

            TempData["Success"] = "Cập nhật trạng thái đơn hàng thành công!";
            return RedirectToAction(nameof(Index));
        }


        // -------------------------------
        // 🧾 1) Invoice → redirect sang InvoiceView
        // -------------------------------
        public IActionResult Invoice(int id)
        {
            return RedirectToAction("InvoiceView", new { id });
        }


        // -------------------------------
        // 🧾 2) HIỂN THỊ HÓA ĐƠN TRÊN TRÌNH DUYỆT
        // -------------------------------
        public async Task<IActionResult> InvoiceView(int id)
        {
            var order = await _db.Orders
                .Include(o => o.Items)
                .ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null) return NotFound();

            order.Total = order.Items.Sum(i => i.UnitPrice * i.Quantity) - order.DiscountAmount;

            return View(order);   // 👉 cần file InvoiceView.cshtml
        }


        // -------------------------------
        // 🧾 3) IN PDF (nếu cần)
        // -------------------------------
        public async Task<IActionResult> InvoicePdf(int id)
        {
            var order = await _db.Orders
                .Include(o => o.Items)
                .ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null) return NotFound();

            order.Total = order.Items.Sum(i => i.UnitPrice * i.Quantity) - order.DiscountAmount;

            return View(order);   // 👉 cần file InvoicePdf.cshtml
        }

    }
}
