using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopNongSan.Data;

namespace ShopNongSan.Areas.Dashboard.Controllers
{
    [Area("Dashboard")]
    [Authorize(Roles = "Admin")]
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _db;

        public HomeController(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<IActionResult> Index()
        {
            ViewBag.TotalProducts = await _db.Products.CountAsync();
            ViewBag.TotalOrders = await _db.Orders.CountAsync();
            ViewBag.PendingOrders = await _db.Orders.CountAsync(o => o.Status == "Pending");
            ViewBag.Revenue = await _db.Orders.SumAsync(o => (decimal?)o.Total) ?? 0m;

            return View();
        }
    }
}
