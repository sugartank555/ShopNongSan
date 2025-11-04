using System.ComponentModel.DataAnnotations;

namespace ShopNongSan.Models
{
    public enum OrderStatus { Pending, Packed, Shipping, Completed, Canceled, Returned }

    public class Order
    {
        public int Id { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        [Required] public string UserId { get; set; } = ""; // IdentityUser.Id
        [StringLength(120)] public string CustomerName { get; set; } = "";
        [StringLength(20)] public string Phone { get; set; } = "";
        [StringLength(300)] public string Address { get; set; } = "";
        public decimal Total { get; set; }
        public OrderStatus Status { get; set; } = OrderStatus.Pending;

        public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
    }
}
