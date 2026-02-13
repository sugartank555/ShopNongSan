using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ShopNongSan.Models
{
    public class Coupon
    {
        public int Id { get; set; }

        [Required, StringLength(20)]
        [Display(Name = "Mã giảm giá")]
        public string Code { get; set; } = string.Empty;

        [Display(Name = "Loại giảm giá")] // "Percent" hoặc "Fixed"
        public string DiscountType { get; set; } = "Percent";

        [Display(Name = "Giá trị giảm")]
        public decimal DiscountValue { get; set; }

        [Display(Name = "Ngày hết hạn")]
        public DateTime ExpiryDate { get; set; }

        [Display(Name = "Kích hoạt")]
        public bool IsActive { get; set; } = true;

        // ✅ Tương thích code cũ có dùng DiscountPercent
        [NotMapped]
        public decimal DiscountPercent
        {
            get => DiscountType == "Percent" ? DiscountValue : 0;
        }
    }
}
