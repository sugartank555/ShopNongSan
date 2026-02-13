using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ShopNongSan.Models
{
    public enum OrderStatus
    {
        Pending = 0,
        Paid = 1,
        Processing = 2,
        Shipped = 3,
        Completed = 4,
        Cancelled = 5,
        Refunded = 6
    }

    public class Order
    {

        public int Id { get; set; }

        [Display(Name = "Ngày tạo")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Display(Name = "Khách hàng")]
        public string? CustomerName { get; set; }

        [Display(Name = "Email")]
        public string? Email { get; set; }

        [Display(Name = "Địa chỉ giao hàng")]
        public string? Address { get; set; }

        [Display(Name = "Số điện thoại")]
        public string? Phone { get; set; }

        [Display(Name = "Trạng thái")]
        public OrderStatus Status { get; set; } = OrderStatus.Pending;

        [Display(Name = "Mã giảm giá (nếu có)")]
        public string? CouponCode { get; set; }

        [Display(Name = "Giảm giá (VNĐ)")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal DiscountAmount { get; set; }

        [Display(Name = "Tổng cộng (sau giảm)")]
        [Column(TypeName = "decimal(18,2)")]

        public decimal Total { get; set; }
        [Display(Name = "Người đặt hàng")]
        public string? UserId { get; set; }  // khóa ngoại đến bảng AspNetUsers
        public DateTime? PaidAt { get; set; }
        public DateTime? ShippedAt { get; set; }
        public DateTime? CompletedAt { get; set; }


        public ICollection<OrderItem>? Items { get; set; }
    }
}
