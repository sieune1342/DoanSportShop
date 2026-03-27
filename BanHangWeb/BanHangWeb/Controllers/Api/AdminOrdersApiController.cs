using BanHangWeb.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BanHangWeb.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class AdminOrdersApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        public AdminOrdersApiController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/AdminOrders
        [HttpGet]
        public async Task<IActionResult> GetOrders()
        {
            var orders = await _context.Orders.OrderByDescending(o => o.OrderDate).ToListAsync();
            return Ok(orders);
        }

        // GET: api/AdminOrders/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetOrder(int id)
        {
            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
                .FirstOrDefaultAsync(o => o.Id == id);
            if (order == null) return NotFound();
            return Ok(order);
        }

        // POST: api/AdminOrders/updatestatus
        [HttpPost("updatestatus")]
        public async Task<IActionResult> UpdateStatus(int id, string orderStatus)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null) return NotFound();
            order.Status = orderStatus;
            _context.Update(order);
            await _context.SaveChangesAsync();
            return Ok(order);
        }
    }
} 