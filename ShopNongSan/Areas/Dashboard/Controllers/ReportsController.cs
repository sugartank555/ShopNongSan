using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopNongSan.Data;

namespace ShopNongSan.Areas.Dashboard.Controllers
{
    [Area("Dashboard")]
    [Authorize(Roles = "Admin")]
    public class ReportsController : Controller
    {
        private readonly ApplicationDbContext _db;
        public ReportsController(ApplicationDbContext db) => _db = db;

        public async Task<IActionResult> Index()
        {
            var totalRevenue = await _db.Orders.Where(o => o.Status == Models.OrderStatus.Completed).SumAsync(o => o.Total);
            var totalOrders = await _db.Orders.CountAsync();
            var lowStock = await _db.Products.Where(p => p.IsActive && p.Price > 0).CountAsync();
            ViewBag.Revenue = totalRevenue;
            ViewBag.Orders = totalOrders;
            ViewBag.LowStock = lowStock;
            return View();
        }
    }
}
