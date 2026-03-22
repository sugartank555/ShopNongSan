using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopNongSan.Data;
using ShopNongSan.Models;
using System.ComponentModel.DataAnnotations;

namespace ShopNongSan.Controllers.Api
{
    [ApiController]
    [Route("api/orders")]
    [Authorize(
        AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme,
        Roles = "Admin,Staff")]
    public class OrdersApiController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        public OrdersApiController(ApplicationDbContext db) => _db = db;

        // ─────────────────────────────────────────────────────────
        // GET api/orders
        // Query: ?status=0 &userId=xxx
        // ─────────────────────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> GetAll(
            [FromQuery] OrderStatus? status,
            [FromQuery] string? userId)
        {
            var query = _db.Orders.Include(o => o.Items).AsQueryable();

            if (status.HasValue)
                query = query.Where(o => o.Status == status.Value);

            if (!string.IsNullOrWhiteSpace(userId))
                query = query.Where(o => o.UserId == userId);

            var data = await query
                .OrderByDescending(o => o.CreatedAt)
                .Select(o => new OrderSummaryResponse
                {
                    Id = o.Id,
                    CustomerName = o.CustomerName,
                    Email = o.Email,
                    Phone = o.Phone,
                    Status = o.Status,
                    StatusLabel = o.Status.ToString(),
                    CouponCode = o.CouponCode,
                    DiscountAmount = o.DiscountAmount,
                    Total = o.Total,
                    CreatedAt = o.CreatedAt,
                    PaidAt = o.PaidAt,
                    ItemCount = o.Items == null ? 0 : o.Items.Count
                })
                .ToListAsync();

            return Ok(data);
        }

        // ─────────────────────────────────────────────────────────
        // GET api/orders/{id}
        // ─────────────────────────────────────────────────────────
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            if (id <= 0)
                return BadRequest(new { message = "Id phải là số nguyên dương." });

            var order = await _db.Orders
                .Include(o => o.Items!)
                    .ThenInclude(i => i.Product)
                .Include(o => o.Items!)
                    .ThenInclude(i => i.BatchLot)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
                return NotFound(new { message = $"Không tìm thấy đơn hàng id={id}." });

            return Ok(new OrderDetailResponse
            {
                Id = order.Id,
                CustomerName = order.CustomerName,
                Email = order.Email,
                Phone = order.Phone,
                Address = order.Address,
                Status = order.Status,
                StatusLabel = order.Status.ToString(),
                CouponCode = order.CouponCode,
                DiscountAmount = order.DiscountAmount,
                Total = order.Total,
                UserId = order.UserId,
                CreatedAt = order.CreatedAt,
                PaidAt = order.PaidAt,
                ShippedAt = order.ShippedAt,
                CompletedAt = order.CompletedAt,
                Items = order.Items?.Select(i => new OrderItemResponse
                {
                    Id = i.Id,
                    ProductId = i.ProductId,
                    ProductName = i.Product?.Name,
                    ProductImage = i.Product?.ImageUrl,
                    BatchLotId = i.BatchLotId,
                    LotCode = i.BatchLot?.LotCode,
                    Quantity = i.Quantity,
                    UnitPrice = i.UnitPrice,
                    LineTotal = i.Quantity * i.UnitPrice
                }).ToList() ?? new()
            });
        }

        // ─────────────────────────────────────────────────────────
        // PATCH api/orders/{id}/status
        // Chỉ cập nhật trạng thái + timestamp tương ứng
        // ─────────────────────────────────────────────────────────
        [HttpPatch("{id:int}/status")]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateOrderStatusRequest dto)
        {
            if (id <= 0)
                return BadRequest(new { message = "Id phải là số nguyên dương." });

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var order = await _db.Orders.FindAsync(id);
            if (order == null)
                return NotFound(new { message = $"Không tìm thấy đơn hàng id={id}." });

            // Không cho chuyển ngược từ Completed / Cancelled / Refunded
            var terminal = new[] { OrderStatus.Completed, OrderStatus.Cancelled, OrderStatus.Refunded };
            if (terminal.Contains(order.Status))
                return UnprocessableEntity(new
                {
                    message = $"Đơn hàng đang ở trạng thái '{order.Status}' — không thể thay đổi tiếp."
                });

            order.Status = dto.Status;

            // Cập nhật timestamp theo trạng thái
            var now = DateTime.UtcNow;
            if (dto.Status == OrderStatus.Paid) order.PaidAt = now;
            if (dto.Status == OrderStatus.Shipped) order.ShippedAt = now;
            if (dto.Status == OrderStatus.Completed) order.CompletedAt = now;

            await _db.SaveChangesAsync();

            return Ok(new
            {
                message = "Cập nhật trạng thái thành công.",
                id = order.Id,
                status = order.Status,
                statusLabel = order.Status.ToString()
            });
        }

        // ─────────────────────────────────────────────────────────
        // DELETE api/orders/{id}  (chỉ Admin, chỉ Pending)
        // ─────────────────────────────────────────────────────────
        [HttpDelete("{id:int}")]
        [Authorize(
            AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme,
            Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            if (id <= 0)
                return BadRequest(new { message = "Id phải là số nguyên dương." });

            var order = await _db.Orders
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
                return NotFound(new { message = $"Không tìm thấy đơn hàng id={id}." });

            if (order.Status != OrderStatus.Pending)
                return UnprocessableEntity(new
                {
                    message = $"Chỉ có thể xóa đơn hàng ở trạng thái Pending. Đơn này đang là '{order.Status}'."
                });

            _db.Orders.Remove(order); // cascade xóa OrderItems theo FK
            await _db.SaveChangesAsync();

            return Ok(new { message = "Đã xóa đơn hàng thành công.", id });
        }
    }

    // ── DTOs ─────────────────────────────────────────────────────
    public class UpdateOrderStatusRequest
    {
        [Required(ErrorMessage = "Status là bắt buộc.")]
        [EnumDataType(typeof(OrderStatus), ErrorMessage = "Giá trị Status không hợp lệ.")]
        public OrderStatus Status { get; set; }
    }

    public class OrderSummaryResponse
    {
        public int Id { get; set; }
        public string? CustomerName { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public OrderStatus Status { get; set; }
        public string StatusLabel { get; set; } = string.Empty;
        public string? CouponCode { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal Total { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? PaidAt { get; set; }
        public int ItemCount { get; set; }
    }

    public class OrderDetailResponse : OrderSummaryResponse
    {
        public string? Address { get; set; }
        public string? UserId { get; set; }
        public DateTime? ShippedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public List<OrderItemResponse> Items { get; set; } = new();
    }

    public class OrderItemResponse
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string? ProductName { get; set; }
        public string? ProductImage { get; set; }
        public int? BatchLotId { get; set; }
        public string? LotCode { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal LineTotal { get; set; }
    }
}
