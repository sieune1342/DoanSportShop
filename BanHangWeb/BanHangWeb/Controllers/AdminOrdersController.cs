using BanHangWeb.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace BanHangWeb.Controllers
{
    public class AdminOrdersController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminOrdersController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /AdminOrders
        public async Task<IActionResult> Index()
        {
            var orders = await _context.Orders
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();
            return View(orders);
        }

        // GET: /AdminOrders/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var order = await _context.Orders
                .Include(o => o.OrderDetails)  // Đảm bảo OrderDetails được tải đầy đủ
                    .ThenInclude(od => od.Product) // Load Product tương ứng
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
                return NotFound();

            return View(order);
        }

        // POST: /AdminOrders/UpdateStatus
        [HttpPost]
        public async Task<IActionResult> UpdateStatus(int id, string orderStatus)
        {
            var order = await _context.Orders.FindAsync(id);

            if (order == null)
                return NotFound();

            // Cập nhật trạng thái đơn hàng
            order.Status = orderStatus;

            // Lưu thay đổi vào cơ sở dữ liệu
            _context.Update(order);
            await _context.SaveChangesAsync();

            // Điều hướng lại đến trang Index sau khi cập nhật trạng thái
            return RedirectToAction(nameof(Index));
        }
    }
}

