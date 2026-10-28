using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProductStore.Models;
using ProductStore.Models.ViewModels;
using ProductStore.Services;
using System.Security.Claims;

namespace ProductStore.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDBContext _context;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IJwtService _jwtService;
        private readonly ICartService _cartService;

        public AccountController(
            ApplicationDBContext context,
            IPasswordHasher passwordHasher,
            IJwtService jwtService,
            ICartService cartService)
        {
            _context = context;
            _passwordHasher = passwordHasher;
            _jwtService = jwtService;
            _cartService = cartService;
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToRoleHome();
            }
            return View(new LoginViewModel { ReturnUrl = returnUrl });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == model.Email.ToLower());
            if (user == null || !_passwordHasher.VerifyPassword(model.Password, user.PasswordHash))
            {
                ModelState.AddModelError(string.Empty, "Invalid email or password.");
                return View(model);
            }

            if (!user.IsActive)
            {
                ModelState.AddModelError(string.Empty, "Your account has been deactivated by the administrator.");
                return View(model);
            }

            // Generate JWT Token
            var token = _jwtService.GenerateToken(user);

            // Store in secure cookie
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = Request.IsHttps,
                SameSite = SameSiteMode.Lax,
                Expires = model.RememberMe ? DateTimeOffset.UtcNow.AddDays(7) : DateTimeOffset.UtcNow.AddHours(12)
            };
            Response.Cookies.Append("jwt_token", token, cookieOptions);

            // Merge guest cart items into user's cart
            await _cartService.MergeCartOnLoginAsync(user.Id);

            if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
            {
                return Redirect(model.ReturnUrl);
            }

            return RedirectToUserRole(user.Role);
        }

        [HttpGet]
        public IActionResult Register()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToRoleHome();
            }
            return View(new RegisterViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var emailExists = await _context.Users.AnyAsync(u => u.Email.ToLower() == model.Email.ToLower());
            if (emailExists)
            {
                ModelState.AddModelError("Email", "An account with this email address already exists.");
                return View(model);
            }

            // Restrict self-registration to Customer or ProductOwner
            var role = (model.Role == UserRoles.ProductOwner) ? UserRoles.ProductOwner : UserRoles.Customer;

            var user = new User
            {
                FullName = model.FullName,
                Email = model.Email.Trim().ToLower(),
                PasswordHash = _passwordHasher.HashPassword(model.Password),
                Role = role,
                PhoneNumber = model.PhoneNumber,
                Address = model.Address,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Auto-login upon registration
            var token = _jwtService.GenerateToken(user);
            Response.Cookies.Append("jwt_token", token, new CookieOptions
            {
                HttpOnly = true,
                Secure = Request.IsHttps,
                SameSite = SameSiteMode.Lax,
                Expires = DateTimeOffset.UtcNow.AddDays(7)
            });

            await _cartService.MergeCartOnLoginAsync(user.Id);

            return RedirectToUserRole(user.Role);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Logout()
        {
            Response.Cookies.Delete("jwt_token");
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            var userId = GetCurrentUserId();
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return RedirectToAction("Login");

            var model = new ProfileViewModel
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                Address = user.Address,
                Role = user.Role,
                CreatedAt = user.CreatedAt
            };

            return View(model);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(ProfileViewModel model)
        {
            var userId = GetCurrentUserId();
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return RedirectToAction("Login");

            if (!ModelState.IsValid)
            {
                model.Role = user.Role;
                model.CreatedAt = user.CreatedAt;
                return View(model);
            }

            user.FullName = model.FullName;
            user.PhoneNumber = model.PhoneNumber;
            user.Address = model.Address;

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Profile updated successfully!";

            return RedirectToAction("Profile");
        }

        // REST API Login Endpoint (for mobile or external apps wanting raw JWT token)
        [HttpPost("api/auth/login")]
        public async Task<IActionResult> ApiLogin([FromBody] LoginViewModel model)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == model.Email.ToLower());
            if (user == null || !_passwordHasher.VerifyPassword(model.Password, user.PasswordHash) || !user.IsActive)
            {
                return Unauthorized(new AuthResponseDto { Success = false, Message = "Invalid credentials or inactive account." });
            }

            var token = _jwtService.GenerateToken(user);
            return Ok(new AuthResponseDto
            {
                Success = true,
                Token = token,
                Role = user.Role,
                FullName = user.FullName,
                Message = "Authentication successful"
            });
        }

        private int GetCurrentUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(claim, out int id) ? id : 0;
        }

        private IActionResult RedirectToRoleHome()
        {
            if (User.IsInRole(UserRoles.Admin)) return RedirectToAction("Index", "Admin");
            if (User.IsInRole(UserRoles.ProductOwner)) return RedirectToAction("Index", "Seller");
            return RedirectToAction("Index", "Home");
        }

        private IActionResult RedirectToUserRole(string role)
        {
            if (role == UserRoles.Admin) return RedirectToAction("Index", "Admin");
            if (role == UserRoles.ProductOwner) return RedirectToAction("Index", "Seller");
            return RedirectToAction("Index", "Home");
        }
    }
}
