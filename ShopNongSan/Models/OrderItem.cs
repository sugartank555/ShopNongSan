using System.ComponentModel.DataAnnotations;

namespace ShopNongSan.Models
{
    public class OrderItem
    {
        public int Id { get; set; }
        [Range(1, 10000)] public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }

        public int ProductId { get; set; }
        public Product? Product { get; set; }

        public int? BatchLotId { get; set; }
        public BatchLot? BatchLot { get; set; }

        public int OrderId { get; set; }
        public Order? Order { get; set; }
    }
}
