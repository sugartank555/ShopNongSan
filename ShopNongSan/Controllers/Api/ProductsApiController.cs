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
    [Route("api/products")]
    [Authorize(
        AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme,
        Roles = "Admin,Staff")]
    public class ProductsApiController : ControllerBase
    {
        private readonly ApplicationDbContext _db;

        public ProductsApiController(ApplicationDbContext db) => _db = db;

        // ─────────────────────────────────────────────────────────
        // GET api/products
        // Query: ?categoryId=1 &isActive=true &isFeatured=true
        // ─────────────────────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> GetAll(
            [FromQuery] int? categoryId,
            [FromQuery] bool? isActive,
            [FromQuery] bool? isFeatured)
        {
            var query = _db.Products
                .Include(p => p.Category)
                .AsQueryable();

            if (categoryId.HasValue)
                query = query.Where(p => p.CategoryId == categoryId.Value);

            if (isActive.HasValue)
                query = query.Where(p => p.IsActive == isActive.Value);

            if (isFeatured.HasValue)
                query = query.Where(p => p.IsFeatured == isFeatured.Value);

            var data = await query
                .OrderByDescending(p => p.CreatedAt)
                .Select(p => new ProductResponse
                {
                    Id = p.Id,
                    Name = p.Name,
                    Description = p.Description,
                    Price = p.Price,
                    ImageUrl = p.ImageUrl,
                    CategoryId = p.CategoryId,
                    CategoryName = p.Category == null ? null : p.Category.Name,
                    IsActive = p.IsActive,
                    IsFeatured = p.IsFeatured,
                    CreatedAt = p.CreatedAt
                })
                .ToListAsync();

            return Ok(data);
        }

        // ─────────────────────────────────────────────────────────
        // GET api/products/{id}
        // ─────────────────────────────────────────────────────────
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            if (id <= 0)
                return BadRequest(new { message = "Id phải là số nguyên dương." });

            var p = await _db.Products
                .Include(x => x.Category)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (p == null)
                return NotFound(new { message = $"Không tìm thấy sản phẩm id={id}." });

            return Ok(new ProductResponse
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
                Price = p.Price,
                ImageUrl = p.ImageUrl,
                CategoryId = p.CategoryId,
                CategoryName = p.Category?.Name,
                IsActive = p.IsActive,
                IsFeatured = p.IsFeatured,
                CreatedAt = p.CreatedAt
            });
        }

        // ─────────────────────────────────────────────────────────
        // POST api/products
        // ─────────────────────────────────────────────────────────
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] ProductRequest dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Kiểm tra category tồn tại và đang active
            var category = await _db.Categories
                .FirstOrDefaultAsync(c => c.Id == dto.CategoryId);

            if (category == null)
                return BadRequest(new { message = $"Không tìm thấy danh mục CategoryId={dto.CategoryId}." });

            if (!category.IsActive)
                return BadRequest(new { message = "Danh mục đã bị vô hiệu hóa." });

            var product = new Product
            {
                Name = dto.Name.Trim(),
                Description = dto.Description?.Trim(),
                Price = dto.Price,
                ImageUrl = dto.ImageUrl,
                CategoryId = dto.CategoryId,
                IsActive = dto.IsActive,
                IsFeatured = dto.IsFeatured,
                CreatedAt = DateTime.UtcNow
            };

            _db.Products.Add(product);
            await _db.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = product.Id }, new
            {
                product.Id,
                product.Name,
                product.Price,
                product.CategoryId,
                product.CreatedAt
            });
        }

        // ─────────────────────────────────────────────────────────
        // PUT api/products/{id}
        // ─────────────────────────────────────────────────────────
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] ProductRequest dto)
        {
            if (id <= 0)
                return BadRequest(new { message = "Id phải là số nguyên dương." });

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var existing = await _db.Products.FindAsync(id);
            if (existing == null)
                return NotFound(new { message = $"Không tìm thấy sản phẩm id={id}." });

            var category = await _db.Categories.FindAsync(dto.CategoryId);
            if (category == null)
                return BadRequest(new { message = $"Không tìm thấy danh mục CategoryId={dto.CategoryId}." });

            existing.Name = dto.Name.Trim();
            existing.Description = dto.Description?.Trim();
            existing.Price = dto.Price;
            existing.ImageUrl = dto.ImageUrl ?? existing.ImageUrl; // giữ ảnh cũ nếu không truyền
            existing.CategoryId = dto.CategoryId;
            existing.IsActive = dto.IsActive;
            existing.IsFeatured = dto.IsFeatured;
            // CreatedAt KHÔNG cập nhật lại

            await _db.SaveChangesAsync();

            return Ok(new
            {
                message = "Cập nhật sản phẩm thành công.",
                existing.Id,
                existing.Name,
                existing.Price
            });
        }

        // ─────────────────────────────────────────────────────────
        // DELETE api/products/{id}   (chỉ Admin)
        // ─────────────────────────────────────────────────────────
        [HttpDelete("{id:int}")]
        [Authorize(
            AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme,
            Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            if (id <= 0)
                return BadRequest(new { message = "Id phải là số nguyên dương." });

            var existing = await _db.Products.FindAsync(id);
            if (existing == null)
                return NotFound(new { message = $"Không tìm thấy sản phẩm id={id}." });

            // Chặn xóa nếu đã có trong đơn hàng
            var hasOrders = await _db.OrderItems.AnyAsync(oi => oi.ProductId == id);
            if (hasOrders)
                return Conflict(new
                {
                    message = "Không thể xóa. Sản phẩm này đã tồn tại trong đơn hàng.",
                    hint = "Hãy dùng IsActive = false để ẩn sản phẩm thay vì xóa."
                });

            // Chặn xóa nếu còn lô hàng liên kết
            var hasBatchLots = await _db.BatchLots.AnyAsync(b => b.ProductId == id);
            if (hasBatchLots)
                return Conflict(new
                {
                    message = "Không thể xóa. Sản phẩm này còn lô hàng (BatchLot) liên kết.",
                    hint = "Xóa các lô hàng trước rồi mới xóa sản phẩm."
                });

            _db.Products.Remove(existing);
            await _db.SaveChangesAsync();

            return Ok(new { message = "Đã xóa sản phẩm thành công.", id });
        }
    }

    // ─────────────────────────────────────────────────────────────
    // DTO — Request (Create / Update)
    // ─────────────────────────────────────────────────────────────
    public class ProductRequest
    {
        [Required(ErrorMessage = "Tên sản phẩm là bắt buộc.")]
        [StringLength(200, ErrorMessage = "Tên không được vượt quá 200 ký tự.")]
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        [Required(ErrorMessage = "Giá bán là bắt buộc.")]
        [Range(0, 999_999_999, ErrorMessage = "Giá phải >= 0.")]
        public decimal Price { get; set; }

        public string? ImageUrl { get; set; }

        [Required(ErrorMessage = "CategoryId là bắt buộc.")]
        [Range(1, int.MaxValue, ErrorMessage = "CategoryId phải là số nguyên dương.")]
        public int CategoryId { get; set; }

        public bool IsActive { get; set; } = true;

        public bool IsFeatured { get; set; } = false;
    }

    // ─────────────────────────────────────────────────────────────
    // DTO — Response (trả về client)
    // ─────────────────────────────────────────────────────────────
    public class ProductResponse
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public string? ImageUrl { get; set; }
        public int CategoryId { get; set; }
        public string? CategoryName { get; set; }
        public bool IsActive { get; set; }
        public bool IsFeatured { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
