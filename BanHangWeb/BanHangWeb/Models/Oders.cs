using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BanHangWeb.Models
{
    public class Order
    {
        public int Id { get; set; }

        [Required]
        public string CustomerName { get; set; }

        [Required]
        public string Address { get; set; }

        [Required, Phone]
        public string Phone { get; set; }

        public string Note { get; set; }

        public DateTime OrderDate { get; set; } = DateTime.Now;

        // 💰 Tổng tiền sản phẩm (chưa tính phí ship)
        public decimal TotalPrice { get; set; }

        // 📨 Email nhận thông báo đơn hàng
        public string Email { get; set; }

        // 📦 Phí vận chuyển
        public decimal ShippingFee { get; set; }

        // 💳 Tổng thanh toán (đã gồm phí ship)
        public decimal TotalPayment { get; set; }

        // 🕓 Trạng thái đơn
        public string Status { get; set; } = "Chờ xác nhận";

        // 🔁 Danh sách chi tiết đơn hàng
        public List<OrderDetail> OrderDetails { get; set; }
    }
}
