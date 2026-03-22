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
    [Route("api/categories")]
    [Authorize(
        AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme,
        Roles = "Admin,Staff")]
    public class CategoriesApiController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        public CategoriesApiController(ApplicationDbContext db) => _db = db;

        // ─────────────────────────────────────────────────────────
        // GET api/categories
        // Query: ?isActive=true
        // ─────────────────────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] bool? isActive)
        {
            var query = _db.Categories.AsQueryable();

            if (isActive.HasValue)
                query = query.Where(c => c.IsActive == isActive.Value);

            var data = await query
                .OrderBy(c => c.Name)
                .Select(c => new CategoryResponse
                {
                    Id = c.Id,
                    Name = c.Name,
                    Slug = c.Slug,
                    Description = c.Description,
                    ImageUrl = c.ImageUrl,
                    IsActive = c.IsActive,
                    ProductCount = c.Products == null ? 0 : c.Products.Count
                })
                .ToListAsync();

            return Ok(data);
        }

        // ─────────────────────────────────────────────────────────
        // GET api/categories/{id}
        // ─────────────────────────────────────────────────────────
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            if (id <= 0)
                return BadRequest(new { message = "Id phải là số nguyên dương." });

            var c = await _db.Categories
                .Include(x => x.Products)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (c == null)
                return NotFound(new { message = $"Không tìm thấy danh mục id={id}." });

            return Ok(new CategoryResponse
            {
                Id = c.Id,
                Name = c.Name,
                Slug = c.Slug,
                Description = c.Description,
                ImageUrl = c.ImageUrl,
                IsActive = c.IsActive,
                ProductCount = c.Products?.Count ?? 0
            });
        }

        // ─────────────────────────────────────────────────────────
        // POST api/categories
        // ─────────────────────────────────────────────────────────
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CategoryRequest dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Kiểm tra trùng tên
            var nameExists = await _db.Categories
                .AnyAsync(c => c.Name.ToLower() == dto.Name.Trim().ToLower());
            if (nameExists)
                return Conflict(new { message = $"Tên danh mục '{dto.Name}' đã tồn tại." });

            var category = new Category
            {
                Name = dto.Name.Trim(),
                Slug = dto.Slug?.Trim(),
                Description = dto.Description?.Trim(),
                ImageUrl = dto.ImageUrl,
                IsActive = dto.IsActive
            };

            _db.Categories.Add(category);
            await _db.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = category.Id }, new
            {
                category.Id,
                category.Name,
                category.IsActive
            });
        }

        // ─────────────────────────────────────────────────────────
        // PUT api/categories/{id}
        // ─────────────────────────────────────────────────────────
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] CategoryRequest dto)
        {
            if (id <= 0)
                return BadRequest(new { message = "Id phải là số nguyên dương." });

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var existing = await _db.Categories.FindAsync(id);
            if (existing == null)
                return NotFound(new { message = $"Không tìm thấy danh mục id={id}." });

            // Kiểm tra trùng tên với danh mục KHÁC
            var nameExists = await _db.Categories
                .AnyAsync(c => c.Name.ToLower() == dto.Name.Trim().ToLower() && c.Id != id);
            if (nameExists)
                return Conflict(new { message = $"Tên danh mục '{dto.Name}' đã được dùng bởi danh mục khác." });

            existing.Name = dto.Name.Trim();
            existing.Slug = dto.Slug?.Trim();
            existing.Description = dto.Description?.Trim();
            existing.ImageUrl = dto.ImageUrl ?? existing.ImageUrl;
            existing.IsActive = dto.IsActive;

            await _db.SaveChangesAsync();

            return Ok(new { message = "Cập nhật danh mục thành công.", existing.Id, existing.Name });
        }

        // ─────────────────────────────────────────────────────────
        // DELETE api/categories/{id}  (chỉ Admin)
        // ─────────────────────────────────────────────────────────
        [HttpDelete("{id:int}")]
        [Authorize(
            AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme,
            Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            if (id <= 0)
                return BadRequest(new { message = "Id phải là số nguyên dương." });

            var existing = await _db.Categories.FindAsync(id);
            if (existing == null)
                return NotFound(new { message = $"Không tìm thấy danh mục id={id}." });

            // Chặn nếu có sản phẩm đang dùng danh mục này
            var hasProducts = await _db.Products.AnyAsync(p => p.CategoryId == id);
            if (hasProducts)
                return Conflict(new
                {
                    message = "Không thể xóa. Danh mục này đang có sản phẩm liên kết.",
                    hint = "Hãy dùng IsActive = false để ẩn danh mục thay vì xóa."
                });

            _db.Categories.Remove(existing);
            await _db.SaveChangesAsync();

            return Ok(new { message = "Đã xóa danh mục thành công.", id });
        }
    }

    // ── DTOs ─────────────────────────────────────────────────────
    public class CategoryRequest
    {
        [Required(ErrorMessage = "Tên danh mục là bắt buộc.")]
        [StringLength(100, MinimumLength = 1, ErrorMessage = "Tên từ 1 đến 100 ký tự.")]
        public string Name { get; set; } = string.Empty;

        [StringLength(200)]
        public string? Slug { get; set; }

        public string? Description { get; set; }

        public string? ImageUrl { get; set; }

        public bool IsActive { get; set; } = true;
    }

    public class CategoryResponse
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Slug { get; set; }
        public string? Description { get; set; }
        public string? ImageUrl { get; set; }
        public bool IsActive { get; set; }
        public int ProductCount { get; set; }
    }
}
