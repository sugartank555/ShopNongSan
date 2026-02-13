using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ShopNongSan.Data;
using ShopNongSan.Models;
using ShopNongSan.Models.ViewModels;
using System.Text.Json;

namespace ShopNongSan.Controllers
{
    [Authorize]
    public class CheckoutController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<IdentityUser> _userMgr;
        public CheckoutController(ApplicationDbContext db, UserManager<IdentityUser> userMgr)
        {
            _db = db;
            _userMgr = userMgr;
        }
        [HttpPost]
        public async Task<IActionResult> CreateOrderOnline()
        {
            var user = await _userMgr.GetUserAsync(User);

            var cartJson = HttpContext.Session.GetString("CART");
            var cart = string.IsNullOrEmpty(cartJson)
                ? new List<CartItem>()
                : JsonSerializer.Deserialize<List<CartItem>>(cartJson)!;

            var subtotal = cart.Sum(x => x.Total);

            var order = new Order
            {
                UserId = user.Id,
                CustomerName = user.Email,
                Address = "Chưa cập nhật",
                Phone = "",
                Status = OrderStatus.Pending,
                Total = subtotal,
                CreatedAt = DateTime.UtcNow
            };

            _db.Orders.Add(order);
            await _db.SaveChangesAsync();

            foreach (var c in cart)
            {
                _db.OrderItems.Add(new OrderItem
                {
                    OrderId = order.Id,
                    ProductId = c.ProductId,
                    Quantity = c.Quantity,
                    UnitPrice = c.Price
                });
            }

            await _db.SaveChangesAsync();

            return Json(new { orderId = order.Id });
        }

        // 🟩 Hiển thị form nhập thông tin giao hàng
        public IActionResult Index()
        {
            var cartJson = HttpContext.Session.GetString("CART");
            var cart = string.IsNullOrEmpty(cartJson)
                ? new List<CartItem>()
                : JsonSerializer.Deserialize<List<CartItem>>(cartJson)!;

            var vm = new CheckoutViewModel
            {
                CartItems = cart
            };
            return View(vm);
        }

        // 🟩 Xác nhận đặt hàng (COD hoặc PayOS)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Confirm(CheckoutViewModel vm)
        {
            if (!ModelState.IsValid)
                return View("Index", vm);

            var user = await _userMgr.GetUserAsync(User);
            var cartJson = HttpContext.Session.GetString("CART");
            if (string.IsNullOrEmpty(cartJson))
                return RedirectToAction("Index", "Cart");

            var cart = JsonSerializer.Deserialize<List<CartItem>>(cartJson)!;
            var subtotal = cart.Sum(x => x.Total);

            // 🟡 Áp mã giảm giá
            var couponJson = HttpContext.Session.GetString("COUPON");
            decimal discount = 0;
            string? couponCode = null;

            if (!string.IsNullOrEmpty(couponJson))
            {
                var coupon = JsonSerializer.Deserialize<Coupon>(couponJson)!;
                couponCode = coupon.Code;

                discount = coupon.DiscountType == "Percent"
                    ? subtotal * coupon.DiscountValue / 100
                    : coupon.DiscountValue;

                if (discount > subtotal) discount = subtotal;
            }

            var totalAfterDiscount = subtotal - discount;

            // 🟢 Lưu đơn hàng trước
            var order = new Order
            {
                UserId = user!.Id,
                CustomerName = vm.FullName,
                Phone = vm.Phone,
                Address = vm.Address,
                CouponCode = couponCode,
                DiscountAmount = discount,
                Total = totalAfterDiscount,
                Status = OrderStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            _db.Orders.Add(order);
            await _db.SaveChangesAsync();

            // 🟢 Lưu chi tiết đơn hàng
            foreach (var i in cart)
            {
                _db.OrderItems.Add(new OrderItem
                {
                    OrderId = order.Id,
                    ProductId = i.ProductId,
                    Quantity = i.Quantity,
                    UnitPrice = i.Price
                });
            }

            await _db.SaveChangesAsync();

            HttpContext.Session.Remove("CART");
            HttpContext.Session.Remove("COUPON");
            // 🟢 Nếu COD → trả về Success
            if (vm.PaymentMethod == "COD")
            {
                return RedirectToAction("Success");
            }

            // 🟢 Nếu PayOS → redirect đến PaymentController
            return RedirectToAction("CreatePayment", "Payment", new
            {
                orderId = order.Id,
                totalAmount = order.Total
            });

        }

        public IActionResult Success() => View();
    }
}
