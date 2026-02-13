using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ShopNongSan.Models
{
    public class Category
    {
        public int Id { get; set; }

        [Required, StringLength(100)]
        [Display(Name = "Tên danh mục")]
        public string Name { get; set; } = string.Empty;

        [StringLength(200)]
        [Display(Name = "Slug (URL thân thiện)")]
        public string? Slug { get; set; }

        [Display(Name = "Mô tả")]
        public string? Description { get; set; }

        // 🆕 Ảnh đại diện cho danh mục
        [Display(Name = "Ảnh đại diện")]
        public string? ImageUrl { get; set; }

        // 🔹 Trạng thái
        [Display(Name = "Hoạt động")]
        public bool IsActive { get; set; } = true;

        // 🔹 Quan hệ 1-n: Category có nhiều Product
        public ICollection<Product>? Products { get; set; }
    }
}
