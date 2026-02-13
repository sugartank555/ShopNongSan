using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopNongSan.Data;

namespace ShopNongSan.Controllers
{
    [Authorize]
    public class OrdersController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<IdentityUser> _userMgr;
        public OrdersController(ApplicationDbContext db, UserManager<IdentityUser> userMgr)
        {
            _db = db; _userMgr = userMgr;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userMgr.GetUserAsync(User);
            var data = await _db.Orders.Where(o => o.UserId == user!.Id)
                .OrderByDescending(o => o.Id).ToListAsync();
            return View(data);
        }

        public async Task<IActionResult> Details(int id)
        {
            var user = await _userMgr.GetUserAsync(User);
            var order = await _db.Orders.Include(o => o.Items).ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(o => o.Id == id && o.UserId == user!.Id);
            if (order == null) return NotFound();
            return View(order);
        }
    }
}
