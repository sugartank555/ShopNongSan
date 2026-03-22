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
    [Route("api/coupons")]
    [Authorize(
        AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme,
        Roles = "Admin,Staff")]
    public class CouponsApiController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        public CouponsApiController(ApplicationDbContext db) => _db = db;

        // ─────────────────────────────────────────────────────────
        // GET api/coupons
        // Query: ?isActive=true
        // ─────────────────────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] bool? isActive)
        {
            var query = _db.Coupons.AsQueryable();

            if (isActive.HasValue)
                query = query.Where(c => c.IsActive == isActive.Value);

            var data = await query
                .OrderByDescending(c => c.Id)
                .Select(c => new CouponResponse
                {
                    Id = c.Id,
                    Code = c.Code,
                    DiscountType = c.DiscountType,
                    DiscountValue = c.DiscountValue,
                    ExpiryDate = c.ExpiryDate,
                    IsActive = c.IsActive,
                    IsExpired = c.ExpiryDate < DateTime.UtcNow
                })
                .ToListAsync();

            return Ok(data);
        }

        // ─────────────────────────────────────────────────────────
        // GET api/coupons/{id}
        // ─────────────────────────────────────────────────────────
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            if (id <= 0)
                return BadRequest(new { message = "Id phải là số nguyên dương." });

            var c = await _db.Coupons.FindAsync(id);
            if (c == null)
                return NotFound(new { message = $"Không tìm thấy coupon id={id}." });

            return Ok(new CouponResponse
            {
                Id = c.Id,
                Code = c.Code,
                DiscountType = c.DiscountType,
                DiscountValue = c.DiscountValue,
                ExpiryDate = c.ExpiryDate,
                IsActive = c.IsActive,
                IsExpired = c.ExpiryDate < DateTime.UtcNow
            });
        }

        // ─────────────────────────────────────────────────────────
        // GET api/coupons/validate/{code}
        // Kiểm tra mã có hợp lệ không (dùng khi checkout)
        // ─────────────────────────────────────────────────────────
        [HttpGet("validate/{code}")]
        public async Task<IActionResult> Validate(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
                return BadRequest(new { message = "Mã giảm giá không được để trống." });

            var coupon = await _db.Coupons
                .FirstOrDefaultAsync(c => c.Code == code.Trim().ToUpper());

            if (coupon == null)
                return NotFound(new { message = "Mã giảm giá không tồn tại." });

            if (!coupon.IsActive)
                return UnprocessableEntity(new { message = "Mã giảm giá đã bị vô hiệu hóa." });

            if (coupon.ExpiryDate < DateTime.UtcNow)
                return UnprocessableEntity(new { message = "Mã giảm giá đã hết hạn." });

            return Ok(new
            {
                code = coupon.Code,
                discountType = coupon.DiscountType,
                discountValue = coupon.DiscountValue,
                expiryDate = coupon.ExpiryDate,
                message = "Mã hợp lệ."
            });
        }

        // ─────────────────────────────────────────────────────────
        // POST api/coupons
        // ─────────────────────────────────────────────────────────
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CouponRequest dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Code lưu uppercase để tránh trùng kiểu chữ hoa/thường
            var code = dto.Code.Trim().ToUpper();

            var codeExists = await _db.Coupons.AnyAsync(c => c.Code == code);
            if (codeExists)
                return Conflict(new { message = $"Mã giảm giá '{code}' đã tồn tại." });

            // Validate DiscountValue theo type
            if (dto.DiscountType == "Percent" && dto.DiscountValue > 100)
                return BadRequest(new { message = "Giảm theo % không thể vượt quá 100." });

            if (dto.ExpiryDate <= DateTime.UtcNow)
                return BadRequest(new { message = "Ngày hết hạn phải là ngày trong tương lai." });

            var coupon = new Coupon
            {
                Code = code,
                DiscountType = dto.DiscountType,
                DiscountValue = dto.DiscountValue,
                ExpiryDate = dto.ExpiryDate,
                IsActive = dto.IsActive
            };

            _db.Coupons.Add(coupon);
            await _db.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = coupon.Id }, new
            {
                coupon.Id,
                coupon.Code,
                coupon.DiscountType,
                coupon.DiscountValue
            });
        }

        // ─────────────────────────────────────────────────────────
        // PUT api/coupons/{id}
        // ─────────────────────────────────────────────────────────
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] CouponRequest dto)
        {
            if (id <= 0)
                return BadRequest(new { message = "Id phải là số nguyên dương." });

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var existing = await _db.Coupons.FindAsync(id);
            if (existing == null)
                return NotFound(new { message = $"Không tìm thấy coupon id={id}." });

            var code = dto.Code.Trim().ToUpper();

            var codeExists = await _db.Coupons.AnyAsync(c => c.Code == code && c.Id != id);
            if (codeExists)
                return Conflict(new { message = $"Mã '{code}' đã được dùng bởi coupon khác." });

            if (dto.DiscountType == "Percent" && dto.DiscountValue > 100)
                return BadRequest(new { message = "Giảm theo % không thể vượt quá 100." });

            existing.Code = code;
            existing.DiscountType = dto.DiscountType;
            existing.DiscountValue = dto.DiscountValue;
            existing.ExpiryDate = dto.ExpiryDate;
            existing.IsActive = dto.IsActive;

            await _db.SaveChangesAsync();

            return Ok(new { message = "Cập nhật coupon thành công.", existing.Id, existing.Code });
        }

        // ─────────────────────────────────────────────────────────
        // DELETE api/coupons/{id}  (chỉ Admin)
        // ─────────────────────────────────────────────────────────
        [HttpDelete("{id:int}")]
        [Authorize(
            AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme,
            Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            if (id <= 0)
                return BadRequest(new { message = "Id phải là số nguyên dương." });

            var existing = await _db.Coupons.FindAsync(id);
            if (existing == null)
                return NotFound(new { message = $"Không tìm thấy coupon id={id}." });

            // Chặn xóa nếu coupon đã được dùng trong đơn hàng
            var usedInOrder = await _db.Orders.AnyAsync(o => o.CouponCode == existing.Code);
            if (usedInOrder)
                return Conflict(new
                {
                    message = "Không thể xóa. Mã giảm giá này đã được sử dụng trong đơn hàng.",
                    hint = "Hãy dùng IsActive = false để vô hiệu hóa thay vì xóa."
                });

            _db.Coupons.Remove(existing);
            await _db.SaveChangesAsync();

            return Ok(new { message = "Đã xóa coupon thành công.", id });
        }
    }

    // ── DTOs ─────────────────────────────────────────────────────
    public class CouponRequest
    {
        [Required(ErrorMessage = "Mã giảm giá là bắt buộc.")]
        [StringLength(20, MinimumLength = 3, ErrorMessage = "Mã từ 3 đến 20 ký tự.")]
        public string Code { get; set; } = string.Empty;

        [Required(ErrorMessage = "Loại giảm giá là bắt buộc.")]
        [RegularExpression("^(Percent|Fixed)$", ErrorMessage = "DiscountType chỉ được là 'Percent' hoặc 'Fixed'.")]
        public string DiscountType { get; set; } = "Percent";

        [Range(0.01, 999_999_999, ErrorMessage = "Giá trị giảm phải > 0.")]
        public decimal DiscountValue { get; set; }

        [Required(ErrorMessage = "Ngày hết hạn là bắt buộc.")]
        public DateTime ExpiryDate { get; set; }

        public bool IsActive { get; set; } = true;
    }

    public class CouponResponse
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string DiscountType { get; set; } = string.Empty;
        public decimal DiscountValue { get; set; }
        public DateTime ExpiryDate { get; set; }
        public bool IsActive { get; set; }
        public bool IsExpired { get; set; }
    }
}
