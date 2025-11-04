using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopNongSan.Data;

namespace ShopNongSan.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _db;
        public HomeController(ApplicationDbContext db) => _db = db;

        public async Task<IActionResult> Index()
        {
            var latest = await _db.Products.Where(p => p.IsActive).OrderByDescending(p => p.Id).Take(8).ToListAsync();
            ViewBag.Latest = latest;
            return View();
        }
    }
}
