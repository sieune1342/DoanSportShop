namespace BanHangWeb.Models
{
    /// <summary>
    /// Đại diện cho một sản phẩm trong giỏ hàng.
    /// </summary>
    public class CartItem
    {
        /// <summary>
        /// ID của sản phẩm.
        /// </summary>
        public int ProductId { get; set; }

        /// <summary>
        /// Tên sản phẩm.
        /// </summary>
        public string ProductName { get; set; } = string.Empty;

        /// <summary>
        /// Đường dẫn ảnh đại diện của sản phẩm.
        /// </summary>
        public string ImageUrl { get; set; } = string.Empty;

        /// <summary>
        /// Đơn giá sản phẩm.
        /// </summary>
        public decimal Price { get; set; }

        /// <summary>
        /// Số lượng sản phẩm trong giỏ.
        /// </summary>
        public int Quantity { get; set; } = 1;

        /// <summary>
        /// Kích cỡ được chọn (Size).
        /// </summary>
        public string Size { get; set; } = string.Empty;
    }
}
