using Microsoft.AspNetCore.Mvc;
using ProductStore.Models.ViewModels;
using ProductStore.Services;

namespace ProductStore.Controllers
{
    public class CartController : Controller
    {
        private readonly ICartService _cartService;

        public CartController(ICartService cartService)
        {
            _cartService = cartService;
        }

        public async Task<IActionResult> Index()
        {
            var items = await _cartService.GetCartItemsAsync();
            var model = new CartViewModel
            {
                Items = items
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddToCart(int productId, int quantity = 1, string? returnUrl = null)
        {
            await _cartService.AddToCartAsync(productId, Math.Max(1, quantity));

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                var newCount = await _cartService.GetCartCountAsync();
                return Json(new { success = true, cartCount = newCount });
            }

            TempData["SuccessMessage"] = "Item added to cart!";

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateQuantity(int cartItemId, int change)
        {
            await _cartService.UpdateQuantityAsync(cartItemId, change);
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Remove(int cartItemId)
        {
            await _cartService.RemoveFromCartAsync(cartItemId);
            TempData["SuccessMessage"] = "Item removed from cart.";
            return RedirectToAction("Index");
        }

        [HttpGet]
        public async Task<IActionResult> Count()
        {
            var count = await _cartService.GetCartCountAsync();
            return Json(new { count });
        }
    }
}
