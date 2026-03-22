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
    [Route("api/batchlots")]
    [Authorize(
        AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme,
        Roles = "Admin,Staff")]
    public class BatchLotsApiController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        public BatchLotsApiController(ApplicationDbContext db) => _db = db;

        // ─────────────────────────────────────────────────────────
        // GET api/batchlots
        // Query: ?productId=1
        // ─────────────────────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] int? productId)
        {
            var query = _db.BatchLots
                .Include(b => b.Product)
                .AsQueryable();

            if (productId.HasValue)
                query = query.Where(b => b.ProductId == productId.Value);

            var data = await query
                .OrderByDescending(b => b.HarvestDate)
                .Select(b => new BatchLotResponse
                {
                    Id = b.Id,
                    LotCode = b.LotCode,
                    FarmName = b.FarmName,
                    HarvestDate = b.HarvestDate,
                    ExpireDate = b.ExpireDate,
                    Certification = b.Certification,
                    QuantityIn = b.QuantityIn,
                    QuantitySold = b.QuantitySold,
                    QuantityLeft = b.QuantityIn - b.QuantitySold,
                    ProductId = b.ProductId,
                    ProductName = b.Product == null ? null : b.Product.Name
                })
                .ToListAsync();

            return Ok(data);
        }

        // ─────────────────────────────────────────────────────────
        // GET api/batchlots/{id}
        // ─────────────────────────────────────────────────────────
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            if (id <= 0)
                return BadRequest(new { message = "Id phải là số nguyên dương." });

            var b = await _db.BatchLots
                .Include(x => x.Product)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (b == null)
                return NotFound(new { message = $"Không tìm thấy lô hàng id={id}." });

            return Ok(new BatchLotResponse
            {
                Id = b.Id,
                LotCode = b.LotCode,
                FarmName = b.FarmName,
                HarvestDate = b.HarvestDate,
                ExpireDate = b.ExpireDate,
                Certification = b.Certification,
                QuantityIn = b.QuantityIn,
                QuantitySold = b.QuantitySold,
                QuantityLeft = b.QuantityIn - b.QuantitySold,
                ProductId = b.ProductId,
                ProductName = b.Product?.Name
            });
        }

        // ─────────────────────────────────────────────────────────
        // POST api/batchlots
        // ─────────────────────────────────────────────────────────
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] BatchLotRequest dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // ExpireDate phải sau HarvestDate
            if (dto.ExpireDate.HasValue && dto.ExpireDate.Value <= dto.HarvestDate)
                return BadRequest(new { message = "Ngày hết hạn phải sau ngày thu hoạch." });

            // Kiểm tra sản phẩm tồn tại và đang active
            var product = await _db.Products.FindAsync(dto.ProductId);
            if (product == null)
                return BadRequest(new { message = $"Không tìm thấy sản phẩm ProductId={dto.ProductId}." });
            if (!product.IsActive)
                return BadRequest(new { message = "Sản phẩm đã bị vô hiệu hóa." });

            // Kiểm tra trùng LotCode
            var codeExists = await _db.BatchLots.AnyAsync(b => b.LotCode == dto.LotCode.Trim());
            if (codeExists)
                return Conflict(new { message = $"Mã lô '{dto.LotCode}' đã tồn tại." });

            var lot = new BatchLot
            {
                LotCode = dto.LotCode.Trim(),
                FarmName = dto.FarmName.Trim(),
                HarvestDate = dto.HarvestDate,
                ExpireDate = dto.ExpireDate,
                Certification = dto.Certification?.Trim(),
                QuantityIn = dto.QuantityIn,
                QuantitySold = 0,          // mới nhập kho, chưa bán
                ProductId = dto.ProductId
            };

            _db.BatchLots.Add(lot);
            await _db.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = lot.Id }, new
            {
                lot.Id,
                lot.LotCode,
                lot.ProductId,
                lot.QuantityIn
            });
        }

        // ─────────────────────────────────────────────────────────
        // PUT api/batchlots/{id}
        // ─────────────────────────────────────────────────────────
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] BatchLotRequest dto)
        {
            if (id <= 0)
                return BadRequest(new { message = "Id phải là số nguyên dương." });

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var existing = await _db.BatchLots.FindAsync(id);
            if (existing == null)
                return NotFound(new { message = $"Không tìm thấy lô hàng id={id}." });

            if (dto.ExpireDate.HasValue && dto.ExpireDate.Value <= dto.HarvestDate)
                return BadRequest(new { message = "Ngày hết hạn phải sau ngày thu hoạch." });

            // QuantityIn không được nhỏ hơn QuantitySold đã có
            if (dto.QuantityIn < existing.QuantitySold)
                return BadRequest(new
                {
                    message = $"QuantityIn ({dto.QuantityIn}) không thể nhỏ hơn số đã bán ({existing.QuantitySold})."
                });

            var product = await _db.Products.FindAsync(dto.ProductId);
            if (product == null)
                return BadRequest(new { message = $"Không tìm thấy sản phẩm ProductId={dto.ProductId}." });

            // Kiểm tra trùng LotCode với lô KHÁC
            var codeExists = await _db.BatchLots
                .AnyAsync(b => b.LotCode == dto.LotCode.Trim() && b.Id != id);
            if (codeExists)
                return Conflict(new { message = $"Mã lô '{dto.LotCode}' đã được dùng bởi lô hàng khác." });

            existing.LotCode = dto.LotCode.Trim();
            existing.FarmName = dto.FarmName.Trim();
            existing.HarvestDate = dto.HarvestDate;
            existing.ExpireDate = dto.ExpireDate;
            existing.Certification = dto.Certification?.Trim();
            existing.QuantityIn = dto.QuantityIn;
            existing.ProductId = dto.ProductId;

            await _db.SaveChangesAsync();

            return Ok(new { message = "Cập nhật lô hàng thành công.", existing.Id, existing.LotCode });
        }

        // ─────────────────────────────────────────────────────────
        // DELETE api/batchlots/{id}  (chỉ Admin)
        // ─────────────────────────────────────────────────────────
        [HttpDelete("{id:int}")]
        [Authorize(
            AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme,
            Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            if (id <= 0)
                return BadRequest(new { message = "Id phải là số nguyên dương." });

            var existing = await _db.BatchLots.FindAsync(id);
            if (existing == null)
                return NotFound(new { message = $"Không tìm thấy lô hàng id={id}." });

            // Chặn nếu lô đã có trong OrderItem
            var hasOrderItems = await _db.OrderItems.AnyAsync(oi => oi.BatchLotId == id);
            if (hasOrderItems)
                return Conflict(new
                {
                    message = "Không thể xóa. Lô hàng này đã được sử dụng trong đơn hàng."
                });

            _db.BatchLots.Remove(existing);
            await _db.SaveChangesAsync();

            return Ok(new { message = "Đã xóa lô hàng thành công.", id });
        }
    }

    // ── DTOs ─────────────────────────────────────────────────────
    public class BatchLotRequest
    {
        [Required(ErrorMessage = "Mã lô là bắt buộc.")]
        [StringLength(40, ErrorMessage = "Mã lô tối đa 40 ký tự.")]
        public string LotCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "Tên trang trại là bắt buộc.")]
        [StringLength(120, ErrorMessage = "Tên trang trại tối đa 120 ký tự.")]
        public string FarmName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Ngày thu hoạch là bắt buộc.")]
        public DateTime HarvestDate { get; set; }

        public DateTime? ExpireDate { get; set; }

        [StringLength(200)]
        public string? Certification { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Số lượng nhập kho phải >= 1.")]
        public int QuantityIn { get; set; }

        [Required(ErrorMessage = "ProductId là bắt buộc.")]
        [Range(1, int.MaxValue, ErrorMessage = "ProductId phải là số nguyên dương.")]
        public int ProductId { get; set; }
    }

    public class BatchLotResponse
    {
        public int Id { get; set; }
        public string LotCode { get; set; } = string.Empty;
        public string FarmName { get; set; } = string.Empty;
        public DateTime HarvestDate { get; set; }
        public DateTime? ExpireDate { get; set; }
        public string? Certification { get; set; }
        public int QuantityIn { get; set; }
        public int QuantitySold { get; set; }
        public int QuantityLeft { get; set; }
        public int ProductId { get; set; }
        public string? ProductName { get; set; }
    }
}
