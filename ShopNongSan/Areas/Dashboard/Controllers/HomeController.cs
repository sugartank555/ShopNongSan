using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopNongSan.Data;

namespace ShopNongSan.Areas.Dashboard.Controllers
{
    [Area("Dashboard")]
    [Authorize(Roles = "Admin,Staff")]
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<IdentityUser> _userManager;

        public HomeController(ApplicationDbContext db, UserManager<IdentityUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            // Tổng quan
            var totalProducts = await _db.Products.CountAsync();
            var totalOrders = await _db.Orders.CountAsync();
            var totalRevenue = await _db.Orders.SumAsync(o => (decimal?)o.Total) ?? 0;
            var totalUsers = (await _userManager.GetUsersInRoleAsync("Customer")).Count;

            ViewBag.TotalProducts = totalProducts;
            ViewBag.TotalOrders = totalOrders;
            ViewBag.TotalRevenue = totalRevenue;
            ViewBag.TotalUsers = totalUsers;

            // 🔹 Doanh thu thật theo tháng (12 tháng gần nhất)
            var now = DateTime.UtcNow;
            var from = new DateTime(now.Year, now.Month, 1).AddMonths(-11); // lùi 12 tháng
            var orders = await _db.Orders
                .Where(o => o.CreatedAt >= from)
                .ToListAsync();

            // Gom nhóm theo tháng/năm
            var revenueData = Enumerable.Range(0, 12)
                .Select(i =>
                {
                    var month = from.AddMonths(i);
                    var total = orders
                        .Where(o => o.CreatedAt.Month == month.Month && o.CreatedAt.Year == month.Year)
                        .Sum(o => o.Total);
                    return new { Month = month.ToString("MM/yyyy"), Total = total };
                })
                .ToList();

            // Chuyển dữ liệu sang ViewBag để Chart.js dùng
            ViewBag.MonthLabels = revenueData.Select(x => x.Month).ToArray();
            ViewBag.MonthRevenue = revenueData.Select(x => x.Total).ToArray();

            return View();
        }
    }
}
