using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProductStore.Models;
using ProductStore.Models.ViewModels;
using ProductStore.Services;
using System.Security.Claims;

namespace ProductStore.Controllers
{
    [Authorize]
    public class CheckoutController : Controller
    {
        private readonly ApplicationDBContext _context;
        private readonly ICartService _cartService;

        public CheckoutController(ApplicationDBContext context, ICartService cartService)
        {
            _context = context;
            _cartService = cartService;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var cartItems = await _cartService.GetCartItemsAsync();
            if (!cartItems.Any())
            {
                TempData["ErrorMessage"] = "Your cart is empty. Please add products before checking out.";
                return RedirectToAction("Index", "Cart");
            }

            var userId = GetCurrentUserId();
            var user = await _context.Users.FindAsync(userId);

            var model = new CheckoutViewModel
            {
                ShippingFullName = user?.FullName ?? string.Empty,
                ShippingEmail = user?.Email ?? string.Empty,
                ShippingPhone = user?.PhoneNumber ?? string.Empty,
                ShippingAddress = user?.Address ?? string.Empty,
                Items = cartItems,
                SubTotal = cartItems.Sum(i => i.Quantity * i.Product.Price),
                ShippingFee = 50.00m
            };
            model.TotalAmount = model.SubTotal + model.ShippingFee;

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PlaceOrder(CheckoutViewModel model)
        {
            var cartItems = await _cartService.GetCartItemsAsync();
            if (!cartItems.Any())
            {
                TempData["ErrorMessage"] = "Your cart is empty.";
                return RedirectToAction("Index", "Cart");
            }

            model.Items = cartItems;
            model.SubTotal = cartItems.Sum(i => i.Quantity * i.Product.Price);
            model.TotalAmount = model.SubTotal + model.ShippingFee;

            // Payment-specific validation
            if (model.PaymentMethod == PaymentMethods.MobileBanking)
            {
                if (string.IsNullOrWhiteSpace(model.MobileAccountNumber))
                {
                    ModelState.AddModelError("MobileAccountNumber", "Sender mobile banking number is required.");
                }
                if (string.IsNullOrWhiteSpace(model.MobileTransactionId))
                {
                    ModelState.AddModelError("MobileTransactionId", "Transaction ID (TrxID) is required.");
                }
            }
            else if (model.PaymentMethod == PaymentMethods.DirectBanking)
            {
                if (string.IsNullOrWhiteSpace(model.BankAccountNumber))
                {
                    ModelState.AddModelError("BankAccountNumber", "Bank account number is required.");
                }
                if (string.IsNullOrWhiteSpace(model.BankTransactionId))
                {
                    ModelState.AddModelError("BankTransactionId", "Bank reference / transaction ID is required.");
                }
            }

            // Check stock availability
            foreach (var item in cartItems)
            {
                if (item.Product.StockQuantity < item.Quantity)
                {
                    ModelState.AddModelError(string.Empty, $"Sorry, '{item.Product.Name}' only has {item.Product.StockQuantity} units in stock.");
                }
            }

            if (!ModelState.IsValid)
            {
                return View("Index", model);
            }

            var userId = GetCurrentUserId();

            // Deduct stock
            foreach (var item in cartItems)
            {
                item.Product.StockQuantity -= item.Quantity;
            }

            // Create Order
            var order = new Order
            {
                OrderNumber = $"ORD-{DateTime.UtcNow:yyyyMMdd}-{Random.Shared.Next(1000, 9999)}",
                UserId = userId,
                OrderDate = DateTime.UtcNow,
                SubTotal = model.SubTotal,
                ShippingFee = model.ShippingFee,
                TotalAmount = model.TotalAmount,
                OrderStatus = OrderStatuses.Pending,
                ShippingFullName = model.ShippingFullName,
                ShippingEmail = model.ShippingEmail,
                ShippingPhone = model.ShippingPhone,
                ShippingAddress = model.ShippingAddress,
                OrderNotes = model.OrderNotes
            };

            foreach (var item in cartItems)
            {
                order.OrderItems.Add(new OrderItem
                {
                    ProductId = item.ProductId,
                    UnitPrice = item.Product.Price,
                    Quantity = item.Quantity,
                    TotalPrice = item.Product.Price * item.Quantity
                });
            }

            // Create Payment
            var payment = new Payment
            {
                Order = order,
                PaymentMethod = model.PaymentMethod,
                Amount = model.TotalAmount,
                Status = PaymentStatuses.Pending,
                PaymentDate = DateTime.UtcNow
            };

            if (model.PaymentMethod == PaymentMethods.MobileBanking)
            {
                payment.Provider = model.MobileProvider ?? "bKash";
                payment.AccountNumber = model.MobileAccountNumber;
                payment.TransactionId = model.MobileTransactionId;
            }
            else if (model.PaymentMethod == PaymentMethods.DirectBanking)
            {
                payment.Provider = model.BankName ?? "City Bank";
                payment.AccountNumber = model.BankAccountNumber;
                payment.TransactionId = model.BankTransactionId;
            }
            else
            {
                payment.Provider = "Cash on Delivery";
            }

            order.Payment = payment;

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            // Clear the cart
            await _cartService.ClearCartAsync();

            return RedirectToAction("Confirmation", new { id = order.Id });
        }

        [HttpGet]
        public async Task<IActionResult> Confirmation(int id)
        {
            var userId = GetCurrentUserId();
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .Include(o => o.Payment)
                .FirstOrDefaultAsync(o => o.Id == id && (o.UserId == userId || User.IsInRole(UserRoles.Admin)));

            if (order == null)
            {
                return NotFound();
            }

            return View(order);
        }

        private int GetCurrentUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(claim, out int id) ? id : 0;
        }
    }
}
