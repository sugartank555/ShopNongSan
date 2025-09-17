using System;
using System.Collections.Generic;

namespace ShopNongSan.Models
{
    public class Order
    {
        public int Id { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Liên kết tài khoản đặt hàng
        public string UserId { get; set; } = default!;
        public ApplicationUser User { get; set; } = default!;

        // Thông tin giao hàng
        public string FullName { get; set; } = "";      // Họ tên người nhận
        public string Address { get; set; } = "";       // Địa chỉ giao hàng
        public string Phone { get; set; } = "";         // SĐT liên hệ

        // Tổng tiền
        public decimal Total { get; set; }

        // Trạng thái đơn
        public string Status { get; set; } = "Pending";
        // Các trạng thái: Pending, Paid, Shipped, Completed, Canceled

        // Danh sách sản phẩm trong đơn
        public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
    }
}
