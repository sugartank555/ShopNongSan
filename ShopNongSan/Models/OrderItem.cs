namespace ShopNongSan.Models
{
    public class OrderItem
    {
        public int Id { get; set; }

        // Số lượng
        public int Quantity { get; set; }

        // Giá tại thời điểm đặt hàng
        public decimal Price { get; set; }

        // Lưu lại tên sản phẩm để không bị thay đổi nếu Product sau này đổi tên
        public string ProductName { get; set; } = "";

        // Khóa ngoại đến Product
        public int ProductId { get; set; }
        public Product Product { get; set; } = default!;

        // Khóa ngoại đến Order
        public int OrderId { get; set; }
        public Order Order { get; set; } = default!;
    }
}
