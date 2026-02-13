using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ShopNongSan.Models
{
    public class Product
    {
        public int Id { get; set; }

        [Required, StringLength(200)]
        [Display(Name = "Tên sản phẩm")]
        public string Name { get; set; } = string.Empty;

        [Display(Name = "Mô tả")]
        public string? Description { get; set; }

        [Display(Name = "Giá bán")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }

        [Display(Name = "Hình ảnh")]
        public string? ImageUrl { get; set; }

        // 🔹 Danh mục
        [Display(Name = "Danh mục")]
        public int CategoryId { get; set; }
        public Category? Category { get; set; }

        // 🔹 Hiển thị
        [Display(Name = "Còn bán")]
        public bool IsActive { get; set; } = true;

        // 🆕 🔹 Sản phẩm nổi bật (hiển thị ở trang chủ)
        [Display(Name = "Sản phẩm nổi bật")]
        public bool IsFeatured { get; set; } = false;

        // 🆕 🔹 Ngày tạo (để sắp xếp sản phẩm mới nhất)
        [Display(Name = "Ngày tạo")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
