using Microsoft.EntityFrameworkCore;
using ProductStore.Models;
using System.Security.Claims;

namespace ProductStore.Services
{
    public interface ICartService
    {
        Task<List<CartItem>> GetCartItemsAsync();
        Task<int> GetCartCountAsync();
        Task AddToCartAsync(int productId, int quantity = 1);
        Task UpdateQuantityAsync(int cartItemId, int change);
        Task RemoveFromCartAsync(int cartItemId);
        Task ClearCartAsync();
        Task MergeCartOnLoginAsync(int userId);
    }

    public class CartService : ICartService
    {
        private readonly ApplicationDBContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private const string CartSessionCookieName = "productstore_cart_session";

        public CartService(ApplicationDBContext context, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        private HttpContext HttpContext => _httpContextAccessor.HttpContext ?? throw new InvalidOperationException("No active HTTP context.");

        private string GetOrCreateSessionId()
        {
            if (HttpContext.Request.Cookies.TryGetValue(CartSessionCookieName, out var sessionId) && !string.IsNullOrEmpty(sessionId))
            {
                return sessionId;
            }

            var newSessionId = Guid.NewGuid().ToString("N");
            HttpContext.Response.Cookies.Append(CartSessionCookieName, newSessionId, new CookieOptions
            {
                Expires = DateTimeOffset.UtcNow.AddDays(30),
                HttpOnly = true,
                SameSite = SameSiteMode.Lax
            });

            return newSessionId;
        }

        private int? GetCurrentUserId()
        {
            var userIdClaim = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(userIdClaim, out int userId))
            {
                return userId;
            }
            return null;
        }

        public async Task<List<CartItem>> GetCartItemsAsync()
        {
            var userId = GetCurrentUserId();
            var sessionId = GetOrCreateSessionId();

            if (userId.HasValue)
            {
                return await _context.CartItems
                    .Include(c => c.Product)
                    .Where(c => c.UserId == userId.Value)
                    .OrderByDescending(c => c.CreatedAt)
                    .ToListAsync();
            }

            return await _context.CartItems
                .Include(c => c.Product)
                .Where(c => c.CartSessionId == sessionId)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();
        }

        public async Task<int> GetCartCountAsync()
        {
            var userId = GetCurrentUserId();
            var sessionId = GetOrCreateSessionId();

            if (userId.HasValue)
            {
                return await _context.CartItems
                    .Where(c => c.UserId == userId.Value)
                    .SumAsync(c => c.Quantity);
            }

            return await _context.CartItems
                .Where(c => c.CartSessionId == sessionId)
                .SumAsync(c => c.Quantity);
        }

        public async Task AddToCartAsync(int productId, int quantity = 1)
        {
            var product = await _context.Products.FindAsync(productId);
            if (product == null || !product.IsActive || product.StockQuantity <= 0) return;

            var userId = GetCurrentUserId();
            var sessionId = GetOrCreateSessionId();

            CartItem? existingItem;
            if (userId.HasValue)
            {
                existingItem = await _context.CartItems
                    .FirstOrDefaultAsync(c => c.UserId == userId.Value && c.ProductId == productId);
            }
            else
            {
                existingItem = await _context.CartItems
                    .FirstOrDefaultAsync(c => c.CartSessionId == sessionId && c.ProductId == productId);
            }

            if (existingItem != null)
            {
                existingItem.Quantity = Math.Min(existingItem.Quantity + quantity, product.StockQuantity);
            }
            else
            {
                var cartItem = new CartItem
                {
                    ProductId = productId,
                    Quantity = Math.Min(quantity, product.StockQuantity),
                    UserId = userId,
                    CartSessionId = userId.HasValue ? null : sessionId,
                    CreatedAt = DateTime.UtcNow
                };
                _context.CartItems.Add(cartItem);
            }

            await _context.SaveChangesAsync();
        }

        public async Task UpdateQuantityAsync(int cartItemId, int change)
        {
            var item = await _context.CartItems.Include(c => c.Product).FirstOrDefaultAsync(c => c.Id == cartItemId);
            if (item == null) return;

            var newQty = item.Quantity + change;
            if (newQty <= 0)
            {
                _context.CartItems.Remove(item);
            }
            else
            {
                item.Quantity = Math.Min(newQty, item.Product.StockQuantity);
            }

            await _context.SaveChangesAsync();
        }

        public async Task RemoveFromCartAsync(int cartItemId)
        {
            var item = await _context.CartItems.FindAsync(cartItemId);
            if (item != null)
            {
                _context.CartItems.Remove(item);
                await _context.SaveChangesAsync();
            }
        }

        public async Task ClearCartAsync()
        {
            var userId = GetCurrentUserId();
            var sessionId = GetOrCreateSessionId();

            var items = userId.HasValue
                ? _context.CartItems.Where(c => c.UserId == userId.Value)
                : _context.CartItems.Where(c => c.CartSessionId == sessionId);

            _context.CartItems.RemoveRange(items);
            await _context.SaveChangesAsync();
        }

        public async Task MergeCartOnLoginAsync(int userId)
        {
            if (HttpContext.Request.Cookies.TryGetValue(CartSessionCookieName, out var sessionId) && !string.IsNullOrEmpty(sessionId))
            {
                var guestItems = await _context.CartItems
                    .Where(c => c.CartSessionId == sessionId)
                    .ToListAsync();

                foreach (var guestItem in guestItems)
                {
                    var userItem = await _context.CartItems
                        .FirstOrDefaultAsync(c => c.UserId == userId && c.ProductId == guestItem.ProductId);

                    if (userItem != null)
                    {
                        userItem.Quantity += guestItem.Quantity;
                        _context.CartItems.Remove(guestItem);
                    }
                    else
                    {
                        guestItem.UserId = userId;
                        guestItem.CartSessionId = null;
                    }
                }

                await _context.SaveChangesAsync();
            }
        }
    }
}
