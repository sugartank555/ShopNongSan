// Models/Order.cs
namespace ShopNongSan.Models
{
    public class Order
    {
        public int Id { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public string UserId { get; set; } = default!;
        public ApplicationUser User { get; set; } = default!;
        public string ShippingAddress { get; set; } = default!;
        public decimal Total { get; set; }
        public string Status { get; set; } = "Pending"; // Pending, Paid, Shipped, Completed, Canceled

        public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
    }
}
