using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BanHangWeb.Models;
using System.Linq;
using System.Threading.Tasks;

namespace BanHangWeb.Controllers
{
    [Authorize]
    public class OrdersController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public OrdersController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // ✅ Hiển thị danh sách đơn hàng của người dùng
        public async Task<IActionResult> MyOrders()
        {
            var user = await _userManager.GetUserAsync(User);
            var email = user?.Email;

            var orders = _context.Orders
                .Where(o => o.Email == email)
                .OrderByDescending(o => o.OrderDate)
                .ToList();

            return View(orders);
        }

        // ✅ Xem chi tiết đơn hàng
        public async Task<IActionResult> Details(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            var email = user?.Email;

            var order = _context.Orders
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
                .FirstOrDefault(o => o.Id == id && o.Email == email); // Kiểm tra đúng user

            if (order == null)
            {
                return NotFound();
            }

            return View(order);
        }
    }
}
