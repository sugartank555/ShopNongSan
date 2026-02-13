using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopNongSan.Data;
using ShopNongSan.Models;
using System.Text.Json;

namespace ShopNongSan.Controllers
{
    public class CartController : Controller
    {
        private readonly ApplicationDbContext _db;
        public CartController(ApplicationDbContext db) => _db = db;

        // ------------------ CART SESSION ------------------
        private List<CartItem> GetCart()
        {
            var session = HttpContext.Session.GetString("CART");
            return string.IsNullOrEmpty(session)
                ? new List<CartItem>()
                : JsonSerializer.Deserialize<List<CartItem>>(session)!;
        }

        private void SaveCart(List<CartItem> cart)
        {
            HttpContext.Session.SetString("CART", JsonSerializer.Serialize(cart));
        }

        private void ClearCoupon()
        {
            HttpContext.Session.Remove("COUPON");
        }

        // ------------------ ACTIONS ------------------

        [HttpPost]
        public async Task<IActionResult> Add(int id, int qty = 1)
        {
            var product = await _db.Products.FindAsync(id);
            if (product == null) return NotFound();

            var cart = GetCart();
            var item = cart.FirstOrDefault(x => x.ProductId == id);
            if (item == null)
                cart.Add(new CartItem
                {
                    ProductId = id,
                    Name = product.Name,
                    Price = product.Price,
                    Image = product.ImageUrl,
                    Quantity = qty
                });
            else
                item.Quantity += qty;

            SaveCart(cart);
            ClearCoupon(); // Khi thêm sp mới, bỏ mã cũ
            TempData["Message"] = "Đã thêm vào giỏ hàng!";
            return RedirectToAction("Index");
        }

        public IActionResult Index()
        {
            var cart = GetCart();
            ViewBag.Coupon = HttpContext.Session.GetString("COUPON");
            return View(cart);
        }

        [HttpPost]
        public IActionResult Update(int id, int qty)
        {
            var cart = GetCart();
            var item = cart.FirstOrDefault(x => x.ProductId == id);
            if (item != null)
            {
                item.Quantity = qty > 0 ? qty : 1;
                SaveCart(cart);
                ClearCoupon();
            }
            return RedirectToAction("Index");
        }

        public IActionResult Remove(int id)
        {
            var cart = GetCart();
            var item = cart.FirstOrDefault(x => x.ProductId == id);
            if (item != null) cart.Remove(item);
            SaveCart(cart);
            ClearCoupon();
            return RedirectToAction("Index");
        }

        public IActionResult Clear()
        {
            HttpContext.Session.Remove("CART");
            HttpContext.Session.Remove("COUPON");
            return RedirectToAction("Index");
        }

        // ------------------ COUPON ------------------
        [HttpPost]
        public async Task<IActionResult> ApplyCoupon(string code)
        {
            code = code?.Trim().ToUpper() ?? "";
            var coupon = await _db.Coupons
                .FirstOrDefaultAsync(c => c.Code == code && c.IsActive && c.ExpiryDate > DateTime.UtcNow);

            if (coupon == null)
            {
                TempData["Error"] = "Mã giảm giá không hợp lệ hoặc đã hết hạn.";
                HttpContext.Session.Remove("COUPON");
            }
            else
            {
                HttpContext.Session.SetString("COUPON", JsonSerializer.Serialize(coupon));
                TempData["Message"] = $"Áp dụng mã '{coupon.Code}' thành công!";
            }
            return RedirectToAction("Index");
        }
    }

    // ------------------ MODEL CARTITEM ------------------
    public class CartItem
    {
        public int ProductId { get; set; }
        public string Name { get; set; } = "";
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public string? Image { get; set; }
        public decimal Total => Price * Quantity;
    }
}
