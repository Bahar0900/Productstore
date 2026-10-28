using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProductStore.Models;
using ProductStore.Models.ViewModels;
using ProductStore.Services;
using System.Security.Claims;

namespace ProductStore.Controllers
{
    [Authorize(Roles = UserRoles.Admin)]
    public class AdminController : Controller
    {
        private readonly ApplicationDBContext _context;

        public AdminController(ApplicationDBContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var totalRevenue = await _context.Orders
                .Where(o => o.OrderStatus != OrderStatuses.Cancelled)
                .SumAsync(o => (decimal?)o.TotalAmount) ?? 0m;

            var totalOrders = await _context.Orders.CountAsync();
            var totalUsers = await _context.Users.CountAsync();
            var totalProducts = await _context.Products.CountAsync();
            var pendingOrders = await _context.Orders.CountAsync(o => o.OrderStatus == OrderStatuses.Pending);
            var pendingPayments = await _context.Payments.CountAsync(p => p.Status == PaymentStatuses.Pending);

            var recentOrders = await _context.Orders
                .Include(o => o.User)
                .Include(o => o.Payment)
                .OrderByDescending(o => o.OrderDate)
                .Take(8)
                .ToListAsync();

            var recentUsers = await _context.Users
                .OrderByDescending(u => u.CreatedAt)
                .Take(5)
                .ToListAsync();

            var viewModel = new AdminDashboardViewModel
            {
                TotalRevenue = totalRevenue,
                TotalOrders = totalOrders,
                TotalUsers = totalUsers,
                TotalProducts = totalProducts,
                PendingOrders = pendingOrders,
                PendingPayments = pendingPayments,
                RecentOrders = recentOrders,
                RecentUsers = recentUsers
            };

            return View(viewModel);
        }

        public async Task<IActionResult> Orders(string? status)
        {
            var query = _context.Orders
                .Include(o => o.User)
                .Include(o => o.Payment)
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .AsQueryable();

            if (!string.IsNullOrEmpty(status) && status != "All")
            {
                query = query.Where(o => o.OrderStatus == status);
            }

            var orders = await query.OrderByDescending(o => o.OrderDate).ToListAsync();
            ViewBag.CurrentFilter = status ?? "All";
            return View(orders);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateOrderStatus(int orderId, string status)
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order != null)
            {
                order.OrderStatus = status;
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Order {order.OrderNumber} status updated to {status}.";
            }
            return RedirectToAction("Orders");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyPayment(int paymentId, string status, string? adminNotes)
        {
            var payment = await _context.Payments.Include(p => p.Order).FirstOrDefaultAsync(p => p.Id == paymentId);
            if (payment != null)
            {
                payment.Status = status;
                payment.AdminNotes = adminNotes;
                if (status == PaymentStatuses.Verified)
                {
                    payment.VerifiedAt = DateTime.UtcNow;
                    if (payment.Order.OrderStatus == OrderStatuses.Pending)
                    {
                        payment.Order.OrderStatus = OrderStatuses.Processing;
                    }
                }
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Payment verification status changed to {status}.";
            }
            return RedirectToAction("Orders");
        }

        public async Task<IActionResult> Users()
        {
            var users = await _context.Users
                .OrderByDescending(u => u.CreatedAt)
                .ToListAsync();

            return View(new UserManagementViewModel { Users = users });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleUserStatus(int id)
        {
            var currentUserId = GetCurrentUserId();
            if (id == currentUserId)
            {
                TempData["ErrorMessage"] = "You cannot deactivate your own admin account!";
                return RedirectToAction("Users");
            }

            var user = await _context.Users.FindAsync(id);
            if (user != null)
            {
                user.IsActive = !user.IsActive;
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"User {user.FullName} is now {(user.IsActive ? "Active" : "Deactivated")}.";
            }
            return RedirectToAction("Users");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeUserRole(int id, string role)
        {
            var currentUserId = GetCurrentUserId();
            if (id == currentUserId)
            {
                TempData["ErrorMessage"] = "You cannot change your own admin role!";
                return RedirectToAction("Users");
            }

            var user = await _context.Users.FindAsync(id);
            if (user != null)
            {
                user.Role = role;
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Role for {user.FullName} changed to {role}.";
            }
            return RedirectToAction("Users");
        }

        // Database View Reporting Query
        public async Task<IActionResult> Reports()
        {
            // Query the SQL database view vw_OrderSummaries mapped in EF Core
            var summaries = await _context.OrderSummaries
                .OrderByDescending(s => s.OrderDate)
                .ToListAsync();

            return View(summaries);
        }

        private int GetCurrentUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(claim, out int id) ? id : 0;
        }
    }
}
