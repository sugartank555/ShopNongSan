using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ShopNongSan.Data;
using ShopNongSan.Models;
using ShopNongSan.Services;
using ShopNongSan.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace ShopNongSan.Controllers
{
    [Authorize] // yêu cầu đăng nhập để đặt hàng
    public class CheckoutController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly ICartSession _cart;
        private readonly UserManager<ApplicationUser> _userMgr;

        public CheckoutController(ApplicationDbContext db, ICartSession cart, UserManager<ApplicationUser> userMgr)
        {
            _db = db; _cart = cart; _userMgr = userMgr;
        }

        [HttpGet]
        public IActionResult Index()
        {
            var cart = _cart.Get();
            if (!cart.Lines.Any()) return RedirectToAction("Index", "Cart");
            return View(cart);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Place(string fullName, string address, string phone)
        {
            var cart = _cart.Get();
            if (!cart.Lines.Any())
                return RedirectToAction("Index", "Cart");

            var user = await _userMgr.GetUserAsync(User);
            if (user == null) return Challenge();

            // Tối giản – tạo Order/OrderItem
            var order = new Order
            {
                UserId = user.Id,
                FullName = fullName,
                Address = address,
                Phone = phone,
                CreatedAt = DateTime.UtcNow,
                Total = cart.Total,
                Items = cart.Lines.Select(l => new OrderItem
                {
                    ProductId = l.ProductId,
                    ProductName = l.Name,
                    Price = l.Price,
                    Quantity = l.Qty
                }).ToList()
            };

            _db.Orders.Add(order);
            await _db.SaveChangesAsync();

            _cart.Clear();
            return RedirectToAction(nameof(Success), new { id = order.Id });
        }

        public async Task<IActionResult> Success(int id)
        {
            var o = await _db.Orders.Include(x => x.Items).FirstOrDefaultAsync(x => x.Id == id);
            if (o == null) return NotFound();
            return View(o);
        }
    }
}
