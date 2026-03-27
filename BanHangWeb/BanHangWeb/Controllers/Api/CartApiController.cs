using BanHangWeb.Models;
using BanHangWeb.Services;
using Microsoft.AspNetCore.Mvc;
using System.Linq;

namespace BanHangWeb.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    public class CartApiController : ControllerBase
    {
        private readonly CartService _cartService;
        private readonly ApplicationDbContext _context;

        public CartApiController(CartService cartService, ApplicationDbContext context)
        {
            _cartService = cartService;
            _context = context;
        }

        // GET: api/Cart
        [HttpGet]
        public IActionResult GetCart()
        {
            var cartItems = _cartService.GetCartItems();

            decimal totalPrice = cartItems.Sum(item => item.Price * item.Quantity);

            return Ok(new
            {
                cartItems,
                totalPrice
                // ❌ Không tính shippingFee hoặc totalPayment tại đây nữa
            });
        }

        // POST: api/Cart/add
        [HttpPost("add")]
        public IActionResult AddToCart(int productId, string selectedSize)
        {
            var product = _context.Products.Find(productId);
            if (product == null) return NotFound();

            if (string.IsNullOrEmpty(selectedSize))
                return BadRequest("Vui lòng chọn kích cỡ.");

            var newItem = new CartItem
            {
                ProductId = product.Id,
                ProductName = product.Name,
                ImageUrl = product.ImageUrl,
                Price = product.Price,
                Quantity = 1,
                Size = selectedSize
            };

            _cartService.AddToCart(newItem);
            return Ok(new { success = true });
        }

        // POST: api/Cart/update
        [HttpPost("update")]
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

            return Ok(new { success = true, totalPrice });
        }

        // POST: api/Cart/remove
        [HttpPost("remove")]
        public IActionResult RemoveFromCart(int productId, string size)
        {
            _cartService.RemoveFromCart(productId, size);
            return Ok(new { success = true });
        }

        // POST: api/Cart/clear
        [HttpPost("clear")]
        public IActionResult ClearCart()
        {
            _cartService.ClearCart();
            return Ok(new { success = true });
        }
    }
}
