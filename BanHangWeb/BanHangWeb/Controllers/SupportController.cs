using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Hosting;
using System.IO;

namespace BanHangWeb.Controllers
{
    // ✅ Bắt buộc phải đăng nhập mới vào bất kỳ action nào trong SupportController
    [Authorize]
    public class SupportController : Controller
    {
        // Trang cho khách hàng (chỉ cần login, không cần role)
        public IActionResult Index()
        {
            return View();
        }

        // Trang cho Admin (login + role Admin)
        [Authorize(Roles = "Admin")]
        public IActionResult Admin()
        {
            return View();
        }
        private readonly IWebHostEnvironment _env;

        public SupportController(IWebHostEnvironment env /*, ApplicationDbContext context, ... */)
        {
            _env = env;
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> UploadImage(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { success = false, error = "File trống" });

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            var allowed = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
            if (!allowed.Contains(ext))
                return BadRequest(new { success = false, error = "Định dạng không hỗ trợ" });

            var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", "support");
            Directory.CreateDirectory(uploadsFolder);

            var fileName = $"{Guid.NewGuid()}{ext}";
            var fullPath = Path.Combine(uploadsFolder, fileName);

            using (var stream = System.IO.File.Create(fullPath))
            {
                await file.CopyToAsync(stream);
            }

            var url = $"/uploads/support/{fileName}";

            return Json(new { success = true, url });
        }
        [Authorize]
        public IActionResult Video()
        {
            return View();
        }
    }
}
