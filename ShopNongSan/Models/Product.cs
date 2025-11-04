using System.ComponentModel.DataAnnotations;

namespace ShopNongSan.Models
{
    public class Product
    {
        public int Id { get; set; }
        [Required, StringLength(120)] public string Name { get; set; } = "";
        [StringLength(160)] public string? Slug { get; set; }
        [StringLength(2000)] public string? Description { get; set; }
        public decimal Price { get; set; }
        public decimal? OriginalPrice { get; set; }
        public string? ImageUrl { get; set; }
        public bool IsActive { get; set; } = true;

        public int CategoryId { get; set; }
        public Category? Category { get; set; }
    }
}
