using BanHangWeb.Models;
using BanHangWeb.Services;
using Microsoft.AspNetCore.Mvc;
using System.Linq;

namespace BanHangWeb.Controllers
{
    public class CartController : Controller
    {
        private readonly CartService _cartService;
        private readonly ApplicationDbContext _context;

        public CartController(CartService cartService, ApplicationDbContext context)
        {
            _cartService = cartService;
            _context = context;
        }

        // ✅ Hiển thị giỏ hàng (KHÔNG tính phí vận chuyển nữa)
        public IActionResult Index()
        {
            var cartItems = _cartService.GetCartItems();
            var totalPrice = cartItems.Sum(item => item.Price * item.Quantity);

            ViewBag.TotalPrice = totalPrice;
            return View(cartItems);
        }

        // ✅ Thêm vào giỏ hàng (chỉ cần Size)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddToCart(int productId, string selectedSize)
        {
            var product = _context.Products.Find(productId);
            if (product == null) return NotFound();

            if (string.IsNullOrEmpty(selectedSize))
            {
                TempData["Error"] = "Vui lòng chọn kích cỡ trước khi thêm vào giỏ hàng.";
                return RedirectToAction("Details", "Products", new { id = productId });
            }

            var cartItem = new CartItem
            {
                ProductId = product.Id,
                ProductName = product.Name,
                ImageUrl = product.ImageUrl ?? "",
                Price = product.Price,
                Quantity = 1,
                Size = selectedSize
            };

            _cartService.AddToCart(cartItem);
            return RedirectToAction("Index");
        }

        // ✅ Cập nhật số lượng (AJAX)
        [HttpPost]
        public IActionResult UpdateCart(int productId, string size, int quantity)
        {
            if (quantity <= 0)
            {
                _cartService.RemoveFromCart(productId, size);
            }
            else
            {
                _cartService.UpdateCart(productId, size, quantity);
            }

            var cartItems = _cartService.GetCartItems();
            var totalPrice = cartItems.Sum(c => c.Price * c.Quantity);

            return Json(new { success = true, totalPrice });
        }

        // ✅ Xóa sản phẩm khỏi giỏ hàng
        [HttpPost]
        public IActionResult RemoveFromCart(int productId, string size)
        {
            _cartService.RemoveFromCart(productId, size);
            return Json(new { success = true });
        }

        // ✅ Xóa toàn bộ giỏ hàng
        [HttpPost]
        public IActionResult ClearCart()
        {
            _cartService.ClearCart();
            return Json(new { success = true });
        }
    }
}
