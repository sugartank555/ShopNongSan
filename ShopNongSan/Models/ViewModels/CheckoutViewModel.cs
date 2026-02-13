using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using ShopNongSan.Controllers;

namespace ShopNongSan.Models.ViewModels
{
    public class CheckoutViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập họ tên")]
        [Display(Name = "Họ tên người nhận")]
        public string FullName { get; set; } = "";

        [Required(ErrorMessage = "Vui lòng nhập số điện thoại")]
        [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
        [Display(Name = "Số điện thoại")]
        public string Phone { get; set; } = "";

        [Required(ErrorMessage = "Vui lòng nhập địa chỉ giao hàng")]
        [Display(Name = "Địa chỉ")]
        public string Address { get; set; } = "";
        public string PaymentMethod { get; set; } = "COD";

        public List<CartItem> CartItems { get; set; } = new();
    }
}
