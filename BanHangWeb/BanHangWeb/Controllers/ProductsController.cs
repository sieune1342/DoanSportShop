using BanHangWeb.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace BanHangWeb.Controllers
{
    public class ProductsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public ProductsController(ApplicationDbContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }

        // ✅ Hiển thị danh sách sản phẩm
        [AllowAnonymous]
        public async Task<IActionResult> Index(string searchString, int? categoryId)
        {
            var query = _context.Products.Include(p => p.Category).AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchString))
            {
                query = query.Where(p => p.Name.Contains(searchString));
            }

            if (categoryId.HasValue && categoryId.Value > 0)
            {
                query = query.Where(p => p.CategoryId == categoryId);
            }

            ViewBag.Categories = new SelectList(_context.Categories, "Id", "Name");
            ViewBag.SelectedCategoryId = categoryId;
            ViewBag.SearchString = searchString;

            var products = await query.ToListAsync();
            return View(products);
        }

        // ✅ Xem chi tiết sản phẩm
        [AllowAnonymous]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var product = await _context.Products
                .Include(p => p.Category)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null) return NotFound();

            return View(product);
        }

        // ✅ Thêm sản phẩm (GET)
        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            ViewData["CategoryId"] = new SelectList(_context.Categories, "Id", "Name");
            return View(new Product());
        }

        // ✅ Thêm sản phẩm (POST)
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Product product)
        {
            if (string.IsNullOrEmpty(product.Sizes))
                product.Sizes = "[]";

            // Lưu ảnh chính
            if (product.ImageFile != null)
            {
                product.ImageUrl = await SaveImage(product.ImageFile);
            }

            // Lưu danh sách ảnh chi tiết
            if (product.UploadDetailImages != null && product.UploadDetailImages.Count > 0)
            {
                product.DetailImages = new List<string>();

                foreach (var image in product.UploadDetailImages)
                {
                    product.DetailImages.Add(await SaveImage(image));
                }
            }

            _context.Add(product);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // ✅ Chỉnh sửa sản phẩm (GET)
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound();

            ViewData["CategoryId"] = new SelectList(_context.Categories, "Id", "Name", product.CategoryId);
            return View(product);
        }

        // ✅ Chỉnh sửa sản phẩm (POST)
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Product product)
        {
            if (id != product.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    var existingProduct = await _context.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);
                    if (existingProduct == null) return NotFound();

                    // Cập nhật ảnh chính nếu có thay đổi
                    if (product.ImageFile != null)
                    {
                        DeleteImage(existingProduct.ImageUrl);
                        product.ImageUrl = await SaveImage(product.ImageFile);
                    }
                    else
                    {
                        product.ImageUrl = existingProduct.ImageUrl;
                    }

                    // Cập nhật ảnh chi tiết nếu có
                    if (product.UploadDetailImages != null && product.UploadDetailImages.Count > 0)
                    {
                        if (existingProduct.DetailImages != null)
                        {
                            foreach (var img in existingProduct.DetailImages)
                            {
                                DeleteImage(img);
                            }
                        }

                        product.DetailImages = new List<string>();
                        foreach (var image in product.UploadDetailImages)
                        {
                            product.DetailImages.Add(await SaveImage(image));
                        }
                    }
                    else
                    {
                        product.DetailImages = existingProduct.DetailImages;
                    }

                    _context.Update(product);
                    await _context.SaveChangesAsync();

                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Products.Any(e => e.Id == product.Id))
                        return NotFound();
                    throw;
                }
            }

            ViewData["CategoryId"] = new SelectList(_context.Categories, "Id", "Name", product.CategoryId);
            return View(product);
        }

        // ✅ Xoá sản phẩm
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == id);
            if (product == null) return NotFound();

            DeleteImage(product.ImageUrl);

            if (product.DetailImages != null)
            {
                foreach (var img in product.DetailImages)
                {
                    DeleteImage(img);
                }
            }

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // ✅ Hàm lưu ảnh
        private async Task<string> SaveImage(IFormFile imageFile)
        {
            string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images");
            Directory.CreateDirectory(uploadsFolder);

            string uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(imageFile.FileName);
            string filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await imageFile.CopyToAsync(stream);
            }

            return "/images/" + uniqueFileName;
        }

        // ✅ Hàm xoá ảnh
        private void DeleteImage(string? imageUrl)
        {
            if (!string.IsNullOrWhiteSpace(imageUrl))
            {
                string fullPath = Path.Combine(_webHostEnvironment.WebRootPath, imageUrl.TrimStart('/'));

                if (System.IO.File.Exists(fullPath))
                {
                    System.IO.File.Delete(fullPath);
                }
            }
        }
    }
}
