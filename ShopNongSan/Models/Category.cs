using System.ComponentModel.DataAnnotations;

namespace ShopNongSan.Models
{
    public class Category
    {
        public int Id { get; set; }
        [Required, StringLength(80)] public string Name { get; set; } = "";
        [Required, StringLength(120)] public string Slug { get; set; } = "";
        public bool IsActive { get; set; } = true;

        public ICollection<Product> Products { get; set; } = new List<Product>();
    }
}
