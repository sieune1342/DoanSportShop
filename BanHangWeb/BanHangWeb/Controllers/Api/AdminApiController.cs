using BanHangWeb.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace BanHangWeb.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class AdminApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        public AdminApiController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("dashboard")]
        public IActionResult Dashboard()
        {
            var totalOrders = _context.Orders.Count();
            var totalRevenue = _context.Orders.Sum(o => o.TotalPrice);
            var totalProducts = _context.Products.Count();
            var totalCustomers = _context.Orders.Select(o => o.Email).Distinct().Count();
            var topProducts = _context.OrderDetails
                .Include(od => od.Product)
                .GroupBy(od => new { od.ProductId, od.Product.Name })
                .Select(g => new { ProductName = g.Key.Name, QuantitySold = g.Sum(od => od.Quantity) })
                .OrderByDescending(g => g.QuantitySold)
                .Take(5)
                .ToList();
            var topCustomers = _context.Orders
                .GroupBy(o => o.Email)
                .Select(g => new { Email = g.Key, TotalSpent = g.Sum(o => o.TotalPrice) })
                .OrderByDescending(g => g.TotalSpent)
                .Take(5)
                .ToList();
            return Ok(new {
                totalOrders,
                totalRevenue,
                totalProducts,
                totalCustomers,
                topProducts,
                topCustomers
            });
        }
    }
} 