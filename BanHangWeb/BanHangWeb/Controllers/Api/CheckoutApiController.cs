using BanHangWeb.Models;
using BanHangWeb.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BanHangWeb.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    public class CheckoutApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly CartService _cartService;
        private readonly EmailService _emailService;

        public CheckoutApiController(ApplicationDbContext context, CartService cartService, EmailService emailService)
        {
            _context = context;
            _cartService = cartService;
            _emailService = emailService;
        }

        // POST: api/CheckoutApi/placeorder
        [HttpPost("placeorder")]
        public async Task<IActionResult> PlaceOrder([FromBody] Order order)
        {
            var cartItems = _cartService.GetCartItems();

            if (!cartItems.Any())
                return BadRequest(new { success = false, message = "Giỏ hàng trống." });

            if (string.IsNullOrEmpty(order.Email))
                return BadRequest(new { success = false, message = "Vui lòng nhập email." });

            if (order.ShippingFee < 0)
                return BadRequest(new { success = false, message = "Phí vận chuyển không hợp lệ." });

            // 💰 Tiền hàng
            order.TotalPrice = cartItems.Sum(item => item.Price * item.Quantity);

            // 🚚 Phí vận chuyển (từ client)
            order.TotalPayment = order.TotalPrice + order.ShippingFee;

            order.OrderDate = DateTime.Now;
            order.Status = "Chờ xác nhận";

            // Chi tiết đơn hàng
            order.OrderDetails = cartItems.Select(item => new OrderDetail
            {
                ProductId = item.ProductId,
                Quantity = item.Quantity,
                Price = item.Price
            }).ToList();

            // Lưu đơn hàng
            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            // Cập nhật tồn kho sản phẩm
            foreach (var detail in order.OrderDetails)
            {
                var product = await _context.Products.FindAsync(detail.ProductId);
                if (product != null)
                {
                    product.Quantity -= detail.Quantity;
                    if (product.Quantity < 0) product.Quantity = 0;
                }
            }
            await _context.SaveChangesAsync();

            // Xoá giỏ hàng
            _cartService.ClearCart();

            // Gửi email xác nhận đơn
            await SendOrderConfirmationEmail(order);

            return Ok(new
            {
                success = true,
                message = "Đặt hàng thành công!",
                orderId = order.Id,
                totalPrice = order.TotalPrice,
                shippingFee = order.ShippingFee,
                totalPayment = order.TotalPayment
            });
        }

        private async Task SendOrderConfirmationEmail(Order order)
        {
            string subject = $"Xác nhận đơn hàng #{order.Id}";
            StringBuilder body = new StringBuilder();

            body.AppendLine($"<h3>Chào {order.CustomerName},</h3>");
            body.AppendLine("<p>Cảm ơn bạn đã đặt hàng tại BanHangWeb. Đây là thông tin đơn hàng của bạn:</p>");
            body.AppendLine($"<p><strong>Mã đơn hàng:</strong> {order.Id}</p>");
            body.AppendLine($"<p><strong>Địa chỉ:</strong> {order.Address}</p>");
            body.AppendLine($"<p><strong>Số điện thoại:</strong> {order.Phone}</p>");

            if (!string.IsNullOrEmpty(order.Note))
                body.AppendLine($"<p><strong>Ghi chú:</strong> {order.Note}</p>");

            body.AppendLine($"<p><strong>🛒 Tiền hàng:</strong> {order.TotalPrice:N0} VND</p>");
            body.AppendLine($"<p><strong>🚚 Phí vận chuyển:</strong> {order.ShippingFee:N0} VND</p>");
            body.AppendLine($"<p><strong>💳 Tổng thanh toán:</strong> <strong style='color:green'>{order.TotalPayment:N0} VND</strong></p>");

            body.AppendLine("<h4>📋 Chi tiết đơn hàng:</h4>");
            body.AppendLine("<ul>");

            foreach (var detail in order.OrderDetails)
            {
                var product = await _context.Products.FindAsync(detail.ProductId);
                if (product != null)
                {
                    body.AppendLine($"<li>{product.Name} - {detail.Quantity} x {detail.Price:N0} VND</li>");
                }
            }

            body.AppendLine("</ul>");
            body.AppendLine("<p>Chúng tôi sẽ xử lý đơn hàng của bạn sớm nhất có thể.</p>");
            body.AppendLine("<p>Trân trọng,</p>");
            body.AppendLine("<p><strong>BanHangWeb Team</strong></p>");

            await _emailService.SendEmailAsync(order.CustomerName, order.Email, subject, body.ToString());
        }

        // GET: api/CheckoutApi/success/5
        [HttpGet("success/{orderId}")]
        public IActionResult Success(int orderId)
        {
            var order = _context.Orders
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
                .FirstOrDefault(o => o.Id == orderId);

            if (order == null)
                return NotFound(new { success = false, message = "Không tìm thấy đơn hàng." });

            return Ok(new
            {
                success = true,
                order.Id,
                order.CustomerName,
                order.Phone,
                order.Address,
                order.TotalPrice,
                order.ShippingFee,
                order.TotalPayment,
                Products = order.OrderDetails.Select(od => new
                {
                    od.ProductId,
                    od.Product.Name,
                    od.Quantity,
                    od.Price
                })
            });
        }
    }
}
