using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Net.payOS;
using Net.payOS.Types;
using ShopNongSan.Data;
using ShopNongSan.Models;

namespace ShopNongSan.Controllers
{
    public class PaymentController : Controller
    {
        private readonly PayOS _payOS;
        private readonly PayOSSettings _settings;
        private readonly ApplicationDbContext _db;

        public PaymentController(IOptions<PayOSSettings> settings, ApplicationDbContext db)
        {
            _settings = settings.Value;

            _payOS = new PayOS(
                clientId: _settings.ClientId,
                apiKey: _settings.ApiKey,
                checksumKey: _settings.ChecksumKey
            );

            _db = db;
        }

        // 🟢 Tạo link thanh toán
        [HttpGet]
        public async Task<IActionResult> CreatePayment(int orderId, decimal totalAmount)
        {
            try
            {
                // 🔹 Mô tả
                string description = $"Thanh toán đơn hàng #{orderId}";

                // 🔹 Danh sách sản phẩm (PayOS yêu cầu price dạng int)
                var items = new List<ItemData>()
                {
                    new ItemData("NongSan Order", 1, (int)totalAmount)
                };

                // 🔹 Dữ liệu gửi PayOS
                var paymentData = new PaymentData(
                    orderCode: orderId,
                    amount: (int)totalAmount,
                    description: description,
                    items: items,
                    returnUrl: _settings.ReturnUrl,
                    cancelUrl: _settings.CancelUrl
                );

                var response = await _payOS.createPaymentLink(paymentData);

                return Redirect(response.checkoutUrl);
            }
            catch (Exception ex)
            {
                Console.WriteLine("PAYOS ERROR: " + ex.Message);
                TempData["Error"] = "Không thể tạo liên kết thanh toán PayOS!";
                return RedirectToAction("Index", "Checkout");
            }
        }

        // 🟢 Trang hiển thị khi thanh toán thành công
        [HttpGet]
        public async Task<IActionResult> Success(int orderCode, string code)
        {
            try
            {
                // 🔎 Kiểm tra trạng thái từ PayOS
                var payment = await _payOS.getPaymentLinkInformation(orderCode);

                if (payment.status == "PAID")
                {
                    // Update đơn hàng
                    var order = await _db.Orders.FirstOrDefaultAsync(x => x.Id == orderCode);
                    if (order != null)
                    {
                        order.Status = OrderStatus.Paid;
                        await _db.SaveChangesAsync();
                    }

                    ViewBag.Message = "Thanh toán thành công!";
                }
                else
                {
                    ViewBag.Message = "Thanh toán chưa hoàn tất.";
                }
            }
            catch
            {
                ViewBag.Message = "Không xác định được trạng thái thanh toán.";
            }

            return View();
        }

        // 🟠 Người dùng hủy thanh toán
        public IActionResult Cancel()
        {
            ViewBag.Message = "Bạn đã hủy thanh toán!";
            return View();
        }
    }
}
