using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;

namespace BanHangWeb.Models
{
    public class Product
    {
        public int Id { get; set; }

        [Required, StringLength(100)]
        public string Name { get; set; }

        [Required]
        public decimal Price { get; set; }

        public string? ImageUrl { get; set; } // Ảnh sản phẩm chính

        [NotMapped]
        public IFormFile? ImageFile { get; set; } // Upload ảnh chính

        [Required]
        public int CategoryId { get; set; }
        public Category? Category { get; set; }

        [Required, StringLength(1000)]
        public string Description { get; set; }

        // ✅ Lưu Size dưới dạng JSON
        public string Sizes { get; set; } = "[]";

        [NotMapped]
        public List<string> SizeList
        {
            get
            {
                try { return JsonConvert.DeserializeObject<List<string>>(Sizes) ?? new List<string>(); }
                catch { return new List<string>(); }
            }
            set => Sizes = JsonConvert.SerializeObject(value);
        }
        // ✅ Lưu Color dưới dạng JSON
        public string Colors { get; set; } = "[]";

        [NotMapped]
        public List<string> ColorList
        {
            get
            {
                try
                {
                    return JsonConvert.DeserializeObject<List<string>>(Colors)
                           ?? new List<string>();
                }
                catch
                {
                    return new List<string>();
                }
            }
            set => Colors = JsonConvert.SerializeObject(value);
        }


        // ✅ Lưu danh sách ảnh chi tiết dưới dạng JSON
        public string DetailImagesJson { get; set; } = "[]";

        [NotMapped]
        public List<string> DetailImages
        {
            get
            {
                try { return JsonConvert.DeserializeObject<List<string>>(DetailImagesJson) ?? new List<string>(); }
                catch { return new List<string>(); }
            }
            set => DetailImagesJson = JsonConvert.SerializeObject(value);
        }

        [NotMapped]
        public List<IFormFile>? UploadDetailImages { get; set; } // Dùng để upload ảnh chi tiết

        // ✅ Quan hệ 1-nhiều với OrderDetails
        public List<OrderDetail>? OrderDetails { get; set; }

        [Required]
        public int Quantity { get; set; } = 0;
    }
}
