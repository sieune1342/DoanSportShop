using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BanHangWeb.Models;
using System;
using System.Globalization;
using System.Linq;

namespace BanHangWeb.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Dashboard()
        {
            var today = DateTime.Today;
            var startOfWeek = today.AddDays(-(int)today.DayOfWeek + (int)DayOfWeek.Monday);
            var startOfMonth = new DateTime(today.Year, today.Month, 1);

            // Đơn hàng hôm nay, tuần này, tháng này
            var ordersToday = _context.Orders.Count(o => o.OrderDate.Date == today);
            var ordersThisWeek = _context.Orders.Count(o => o.OrderDate.Date >= startOfWeek);
            var ordersThisMonth = _context.Orders.Count(o => o.OrderDate.Date >= startOfMonth);

            // Doanh thu hôm nay, tuần này, tháng này
            var revenueToday = _context.Orders.Where(o => o.OrderDate.Date == today).Sum(o => (decimal?)o.TotalPrice) ?? 0;
            var revenueThisWeek = _context.Orders.Where(o => o.OrderDate.Date >= startOfWeek).Sum(o => (decimal?)o.TotalPrice) ?? 0;
            var revenueThisMonth = _context.Orders.Where(o => o.OrderDate.Date >= startOfMonth).Sum(o => (decimal?)o.TotalPrice) ?? 0;

            // Doanh thu 7 ngày gần nhất
            var last7Days = Enumerable.Range(0, 7).Select(i => today.AddDays(-i)).Reverse().ToList();
            var revenueLast7Days = last7Days.Select(d => new {
                Date = d.ToString("dd/MM"),
                Revenue = _context.Orders.Where(o => o.OrderDate.Date == d).Sum(o => (decimal?)o.TotalPrice) ?? 0
            }).ToList();
            ViewBag.RevenueLast7DaysLabels = revenueLast7Days.Select(x => x.Date).ToList();
            ViewBag.RevenueLast7DaysData = revenueLast7Days.Select(x => x.Revenue).ToList();

            // Đơn hàng theo trạng thái
            var statusList = new[] { "Chờ xác nhận", "Đang giao", "Hoàn thành", "Đã hủy" };
            var ordersByStatus = statusList.Select(s => new {
                Status = s,
                Count = _context.Orders.Count(o => o.Status == s)
            }).ToList();
            ViewBag.OrdersByStatusLabels = ordersByStatus.Select(x => x.Status).ToList();
            ViewBag.OrdersByStatusData = ordersByStatus.Select(x => x.Count).ToList();

            // Sản phẩm sắp hết hàng (Quantity < 5)
            var lowStockProducts = _context.Products.Where(p => p.Quantity < 5).Select(p => new { p.Name, p.Quantity }).ToList();
            ViewBag.LowStockProducts = lowStockProducts;

            // Tổng số đơn hàng
            var totalOrders = _context.Orders.Count();

            // Tổng doanh thu
            var totalRevenue = _context.Orders.Sum(o => o.TotalPrice);

            // Tổng số sản phẩm
            var totalProducts = _context.Products.Count();

            // Tổng số khách hàng (dựa trên email duy nhất)
            var totalCustomers = _context.Orders
                .Select(o => o.Email)
                .Distinct()
                .Count();

            // Top 5 sản phẩm bán chạy nhất
            var topProducts = _context.OrderDetails
                .Include(od => od.Product)
                .GroupBy(od => new { od.ProductId, od.Product.Name })
                .Select(g => new
                {
                    ProductName = g.Key.Name,
                    QuantitySold = g.Sum(od => od.Quantity)
                })
                .OrderByDescending(g => g.QuantitySold)
                .Take(5)
                .ToList();

            // Top khách hàng theo tổng chi tiêu (dựa trên Email)
            var topCustomers = _context.Orders
                .GroupBy(o => o.Email)
                .Select(g => new
                {
                    Email = g.Key,
                    TotalSpent = g.Sum(o => o.TotalPrice)
                })
                .OrderByDescending(g => g.TotalSpent)
                .Take(5)
                .ToList();

            // Dữ liệu cho biểu đồ
            ViewBag.TopProductNames = topProducts.Select(p => p.ProductName).ToList();
            ViewBag.TopProductQuantities = topProducts.Select(p => p.QuantitySold).ToList();
            ViewBag.TopCustomerEmails = topCustomers.Select(c => c.Email).ToList();
            ViewBag.TopCustomerSpending = topCustomers.Select(c => c.TotalSpent).ToList();

            // Gửi dữ liệu qua ViewBag
            ViewBag.TotalOrders = totalOrders;
            ViewBag.TotalRevenue = totalRevenue;
            ViewBag.TotalProducts = totalProducts;
            ViewBag.TotalCustomers = totalCustomers;
            ViewBag.TopProducts = topProducts;
            ViewBag.TopCustomers = topCustomers;
            // Thống kê nâng cao
            ViewBag.OrdersToday = ordersToday;
            ViewBag.OrdersThisWeek = ordersThisWeek;
            ViewBag.OrdersThisMonth = ordersThisMonth;
            ViewBag.RevenueToday = revenueToday;
            ViewBag.RevenueThisWeek = revenueThisWeek;
            ViewBag.RevenueThisMonth = revenueThisMonth;
            return View();
        }
    }
}
