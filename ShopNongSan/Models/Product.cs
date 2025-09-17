// Models/Product.cs
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;

namespace ShopNongSan.Models
{
    public class Product
    {
        public int Id { get; set; }
        public string Name { get; set; } = default!;
        public string? Slug { get; set; }
        public string? Description { get; set; }
        public string? ImageUrl { get; set; }
        public decimal Price { get; set; }        // Giá bán
        public int Stock { get; set; }            // Tồn kho
        [Required]
        public int CategoryId { get; set; }
        [ValidateNever]
        public Category Category { get; set; } = default!;

        public bool IsOrganic { get; set; } = true; // Hữu cơ/sạch

    }
}
