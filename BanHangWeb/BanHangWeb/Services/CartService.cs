using BanHangWeb.Models;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;

namespace BanHangWeb.Services
{
    public class CartService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private const string CartSessionKey = "Cart";

        public CartService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        // Lấy danh sách sản phẩm trong giỏ
        public List<CartItem> GetCartItems()
        {
            var session = _httpContextAccessor.HttpContext.Session;
            var cartJson = session.GetString(CartSessionKey);
            return cartJson != null
                ? JsonConvert.DeserializeObject<List<CartItem>>(cartJson)
                : new List<CartItem>();
        }

        // Thêm sản phẩm vào giỏ
        public void AddToCart(CartItem item)
        {
            var cart = GetCartItems();
            var existingItem = cart.FirstOrDefault(p =>
                p.ProductId == item.ProductId &&
                p.Size == item.Size
            );

            if (existingItem != null)
            {
                existingItem.Quantity += item.Quantity;
            }
            else
            {
                cart.Add(item);
            }

            SaveCart(cart);
        }

        // Cập nhật số lượng
        public void UpdateCart(int productId, string size, int quantity)
        {
            var cart = GetCartItems();
            var item = cart.FirstOrDefault(p =>
                p.ProductId == productId &&
                p.Size == size
            );

            if (item != null)
            {
                item.Quantity = quantity;
                if (item.Quantity <= 0)
                    cart.Remove(item);
            }

            SaveCart(cart);
        }

        // Xoá sản phẩm khỏi giỏ
        public void RemoveFromCart(int productId, string size)
        {
            var cart = GetCartItems();
            var itemToRemove = cart.FirstOrDefault(p =>
                p.ProductId == productId &&
                p.Size == size
            );

            if (itemToRemove != null)
            {
                cart.Remove(itemToRemove);
            }

            SaveCart(cart);
        }

        // Xoá toàn bộ giỏ hàng
        public void ClearCart()
        {
            SaveCart(new List<CartItem>());
        }

        // Lưu giỏ hàng vào session
        private void SaveCart(List<CartItem> cart)
        {
            var session = _httpContextAccessor.HttpContext.Session;
            var cartJson = JsonConvert.SerializeObject(cart);
            session.SetString(CartSessionKey, cartJson);
        }
    }
}
