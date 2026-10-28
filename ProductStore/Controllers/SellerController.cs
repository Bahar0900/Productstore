using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProductStore.Models;
using ProductStore.Models.ViewModels;
using ProductStore.Services;
using System.Security.Claims;

namespace ProductStore.Controllers
{
    [Authorize(Roles = $"{UserRoles.ProductOwner},{UserRoles.Admin}")]
    public class SellerController : Controller
    {
        private readonly ApplicationDBContext _context;

        public SellerController(ApplicationDBContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var userId = GetCurrentUserId();

            var myProducts = await _context.Products
                .Where(p => p.OwnerId == userId)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            var productIds = myProducts.Select(p => p.Id).ToList();

            var mySales = await _context.OrderItems
                .Include(oi => oi.Order)
                .Include(oi => oi.Product)
                .Where(oi => productIds.Contains(oi.ProductId))
                .OrderByDescending(oi => oi.Order.OrderDate)
                .ToListAsync();

            var lowStockCount = myProducts.Count(p => p.StockQuantity <= 5);
            var totalSoldUnits = mySales.Sum(s => s.Quantity);
            var totalEarnings = mySales.Sum(s => s.TotalPrice);

            var viewModel = new SellerDashboardViewModel
            {
                TotalProducts = myProducts.Count,
                LowStockProducts = lowStockCount,
                TotalItemsSold = totalSoldUnits,
                TotalEarnings = totalEarnings,
                MyProducts = myProducts,
                RecentSales = mySales.Take(10).ToList()
            };

            return View(viewModel);
        }

        public async Task<IActionResult> SalesOrders()
        {
            var userId = GetCurrentUserId();
            var myProductIds = await _context.Products
                .Where(p => p.OwnerId == userId)
                .Select(p => p.Id)
                .ToListAsync();

            var salesItems = await _context.OrderItems
                .Include(oi => oi.Order)
                .ThenInclude(o => o.Payment)
                .Include(oi => oi.Product)
                .Where(oi => myProductIds.Contains(oi.ProductId))
                .OrderByDescending(oi => oi.Order.OrderDate)
                .ToListAsync();

            return View(salesItems);
        }

        private int GetCurrentUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(claim, out int id) ? id : 0;
        }
    }
}
