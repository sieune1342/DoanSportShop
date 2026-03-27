using BanHangWeb.Models;
using BanHangWeb.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BanHangWeb.Controllers
{
    public class CheckoutController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly CartService _cartService;
        private readonly EmailService _emailService;

        public CheckoutController(ApplicationDbContext context, CartService cartService, EmailService emailService)
        {
            _context = context;
            _cartService = cartService;
            _emailService = emailService;
        }

        // Hiển thị trang thanh toán
        public IActionResult Index()
        {
            var cartItems = _cartService.GetCartItems();

            if (!cartItems.Any())
            {
                return RedirectToAction("Index", "Cart");
            }

            decimal total = cartItems.Sum(item => item.Price * item.Quantity);
            ViewBag.TotalPrice = total; // ✅ Truyền tổng tiền hàng cho view

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> PlaceOrder(Order order)
        {
            var cartItems = _cartService.GetCartItems();

            if (!cartItems.Any())
            {
                return RedirectToAction("Index", "Cart");
            }

            if (string.IsNullOrEmpty(order.Email))
            {
                ModelState.AddModelError("Email", "Vui lòng nhập email để nhận thông báo.");
                return View("Index", order);
            }

            // ✅ Tính tổng tiền hàng
            order.TotalPrice = cartItems.Sum(item => item.Price * item.Quantity);
            order.OrderDate = DateTime.Now;

            // ✅ Gộp danh sách chi tiết sản phẩm
            order.OrderDetails = cartItems.Select(item => new OrderDetail
            {
                ProductId = item.ProductId,
                Quantity = item.Quantity,
                Price = item.Price
            }).ToList();

            // ✅ Nếu người dùng chưa chọn vị trí => ShippingFee và TotalPayment = 0
            if (order.TotalPayment == 0)
            {
                order.TotalPayment = order.TotalPrice + order.ShippingFee;
            }

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            // ✅ Cập nhật tồn kho
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

            // ✅ Xoá giỏ hàng
            _cartService.ClearCart();

            // ✅ Gửi email xác nhận
            await SendOrderConfirmationEmail(order);

            return RedirectToAction("Success", new { orderId = order.Id });
        }

        private async Task SendOrderConfirmationEmail(Order order)
        {
            string subject = $"Xác nhận đơn hàng #{order.Id}";

            StringBuilder body = new StringBuilder();
            body.AppendLine($"<h3>Chào {order.CustomerName},</h3>");
            body.AppendLine($"<p>Cảm ơn bạn đã đặt hàng tại BanHangWeb. Đây là thông tin đơn hàng của bạn:</p>");
            body.AppendLine($"<p><strong>Mã đơn hàng:</strong> {order.Id}</p>");
            body.AppendLine($"<p><strong>Địa chỉ:</strong> {order.Address}</p>");
            body.AppendLine($"<p><strong>Số điện thoại:</strong> {order.Phone}</p>");
            body.AppendLine($"<p><strong>Ghi chú:</strong> {order.Note}</p>");
            body.AppendLine($"<p><strong>Tổng tiền hàng:</strong> {order.TotalPrice:N0} VND</p>");
            body.AppendLine($"<p><strong>Phí ship:</strong> {order.ShippingFee:N0} VND</p>");
            body.AppendLine($"<p><strong>Tổng thanh toán:</strong> {order.TotalPayment:N0} VND</p>");

            body.AppendLine("<h4>🛒 Chi tiết đơn hàng:</h4>");
            body.AppendLine("<ul>");

            foreach (var detail in order.OrderDetails)
            {
                var product = _context.Products.Find(detail.ProductId);
                if (product != null)
                {
                    body.AppendLine($"<li>{product.Name} - {detail.Quantity} x {detail.Price:N0} VND</li>");
                }
            }

            body.AppendLine("</ul>");
            body.AppendLine("<p>Chúng tôi sẽ xử lý đơn hàng của bạn sớm nhất có thể.</p>");
            body.AppendLine("<p>Trân trọng,</p>");
            body.AppendLine("<p>BanHangWeb Team</p>");

            await _emailService.SendEmailAsync(order.CustomerName, order.Email, subject, body.ToString());
        }

        // Trang thông báo thành công
        public IActionResult Success(int orderId)
        {
            var order = _context.Orders
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
                .FirstOrDefault(o => o.Id == orderId);

            if (order == null) return NotFound();

            return View(order);
        }
    }
}
