using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace BanHangWeb.Models
{
    public class Category
    {
        public int Id { get; set; }

        [Required, StringLength(100)]
        public string Name { get; set; }

        public List<Product>? Products { get; set; } // Danh sách sản phẩm thuộc danh mục này
    }
}
