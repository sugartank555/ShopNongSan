// ViewModels/StoreVM.cs
using ShopNongSan.Models;
using System.Collections.Generic;

namespace ShopNongSan.ViewModels
{
    public class StoreHomeVM
    {
        public IEnumerable<Category> Categories { get; set; } = new List<Category>();
        public IEnumerable<Product> Featured { get; set; } = new List<Product>();
        public string? Q { get; set; }
        public int? CategoryId { get; set; }
    }

    public class ProductDetailVM
    {
        public Product Product { get; set; } = default!;
        public IEnumerable<Product> Related { get; set; } = new List<Product>();
    }
}
