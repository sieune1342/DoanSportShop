using BanHangWeb.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace BanHangWeb.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    public class HomeApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        public HomeApiController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/Home
        [HttpGet]
        public IActionResult GetLatestProducts()
        {
            var products = _context.Products
                .Include(p => p.Category)
                .OrderByDescending(p => p.Id)
                .Take(8)
                .ToList();
            return Ok(products);
        }
    }
} 