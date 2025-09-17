// ViewModels/CartVM.cs
using System.Collections.Generic;
using System.Linq;

namespace ShopNongSan.ViewModels
{
    public class CartLineVM
    {
        public int ProductId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public decimal Price { get; set; }
        public int Qty { get; set; }
        public decimal Subtotal => Price * Qty;
    }

    public class CartVM
    {
        public List<CartLineVM> Lines { get; set; } = new();
        public decimal Total => Lines.Sum(x => x.Subtotal);

        // (tuỳ chọn) các helper
        public void Add(int productId, string name, string? imageUrl, decimal price, int qty = 1)
        {
            var line = Lines.FirstOrDefault(x => x.ProductId == productId);
            if (line == null)
                Lines.Add(new CartLineVM { ProductId = productId, Name = name, ImageUrl = imageUrl, Price = price, Qty = qty });
            else
                line.Qty += qty;
        }
        public void Set(int productId, int qty)
        {
            var line = Lines.FirstOrDefault(x => x.ProductId == productId);
            if (line != null) line.Qty = qty <= 0 ? 1 : qty;
        }
        public void Change(int productId, int delta)
        {
            var line = Lines.FirstOrDefault(x => x.ProductId == productId);
            if (line != null)
            {
                line.Qty += delta;
                if (line.Qty <= 0) Lines.Remove(line);
            }
        }
        public void Remove(int productId) => Lines.RemoveAll(x => x.ProductId == productId);
        public void Clear() => Lines.Clear();
    }
}
