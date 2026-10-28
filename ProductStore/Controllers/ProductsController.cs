using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProductStore.Models;
using ProductStore.Services;
using System.Security.Claims;

namespace ProductStore.Controllers
{
    public class ProductsController : Controller
    {
        private readonly ApplicationDBContext context;
        private readonly IWebHostEnvironment environment;

        public ProductsController(ApplicationDBContext context, IWebHostEnvironment environment)
        {
            this.context = context;
            this.environment = environment;
        }

        // Public catalog index
        public async Task<IActionResult> Index()
        {
            var products = await context.Products
                .Include(p => p.Owner)
                .OrderByDescending(p => p.Id)
                .ToListAsync();

            return View(products);
        }

        // Public Product Details View
        public async Task<IActionResult> Details(int id)
        {
            var product = await context.Products
                .Include(p => p.Owner)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }

        // Create: Admin or ProductOwner only
        [Authorize(Roles = $"{UserRoles.Admin},{UserRoles.ProductOwner}")]
        public IActionResult Create()
        {
            return View();
        }

        [Authorize(Roles = $"{UserRoles.Admin},{UserRoles.ProductOwner}")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ProductDto productDto)
        {
            if (productDto.ImageFile == null)
            {
                ModelState.AddModelError("ImageFile", "The image file is required");
            }

            if (!ModelState.IsValid)
            {
                return View(productDto);
            }

            // Create unique filename
            string newFileName = DateTime.UtcNow.ToString("yyyyMMddHHmmssfff") + Path.GetExtension(productDto.ImageFile!.FileName);
            string imageFullPath = Path.Combine(environment.WebRootPath, "ProductImages", newFileName);

            Directory.CreateDirectory(Path.GetDirectoryName(imageFullPath)!);

            using (var stream = System.IO.File.Create(imageFullPath))
            {
                await productDto.ImageFile.CopyToAsync(stream);
            }

            var userId = GetCurrentUserId();

            Product product = new Product()
            {
                Name = productDto.Name,
                Brand = productDto.Brand,
                Category = productDto.Category,
                Price = productDto.Price,
                Description = productDto.Description,
                StockQuantity = productDto.StockQuantity,
                IsActive = productDto.IsActive,
                ImageFileName = newFileName,
                OwnerId = userId,
                CreatedAt = DateTime.UtcNow
            };

            context.Products.Add(product);
            await context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Product launched successfully!";

            if (User.IsInRole(UserRoles.ProductOwner) && !User.IsInRole(UserRoles.Admin))
            {
                return RedirectToAction("Index", "Seller");
            }

            return RedirectToAction("Index", "Products");
        }

        // Edit: Admin can edit any product; ProductOwner can only edit their own
        [Authorize(Roles = $"{UserRoles.Admin},{UserRoles.ProductOwner}")]
        public async Task<IActionResult> Edit(int id)
        {
            var product = await context.Products.FindAsync(id);
            if (product == null)
            {
                return RedirectToAction("Index", "Products");
            }

            var userId = GetCurrentUserId();
            if (!User.IsInRole(UserRoles.Admin) && product.OwnerId != userId)
            {
                return Forbid();
            }

            var productDto = new ProductDto()
            {
                Name = product.Name,
                Brand = product.Brand,
                Category = product.Category,
                Price = product.Price,
                Description = product.Description,
                StockQuantity = product.StockQuantity,
                IsActive = product.IsActive
            };

            ViewData["ProductId"] = product.Id;
            ViewData["ImageFileName"] = product.ImageFileName;
            ViewData["CreatedAt"] = product.CreatedAt.ToString("MM/dd/yyyy");

            return View(productDto);
        }

        [Authorize(Roles = $"{UserRoles.Admin},{UserRoles.ProductOwner}")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ProductDto productDto)
        {
            var product = await context.Products.FindAsync(id);
            if (product == null)
            {
                return RedirectToAction("Index", "Products");
            }

            var userId = GetCurrentUserId();
            if (!User.IsInRole(UserRoles.Admin) && product.OwnerId != userId)
            {
                return Forbid();
            }

            if (!ModelState.IsValid)
            {
                ViewData["ProductId"] = product.Id;
                ViewData["ImageFileName"] = product.ImageFileName;
                ViewData["CreatedAt"] = product.CreatedAt.ToString("MM/dd/yyyy");
                return View(productDto);
            }

            string newFileName = product.ImageFileName;
            if (productDto.ImageFile != null)
            {
                newFileName = DateTime.UtcNow.ToString("yyyyMMddHHmmssfff") + Path.GetExtension(productDto.ImageFile.FileName);
                string imageFullPath = Path.Combine(environment.WebRootPath, "ProductImages", newFileName);

                using (var stream = System.IO.File.Create(imageFullPath))
                {
                    await productDto.ImageFile.CopyToAsync(stream);
                }

                // Delete old image
                if (!string.IsNullOrEmpty(product.ImageFileName))
                {
                    string oldImageFullPath = Path.Combine(environment.WebRootPath, "ProductImages", product.ImageFileName);
                    if (System.IO.File.Exists(oldImageFullPath))
                    {
                        System.IO.File.Delete(oldImageFullPath);
                    }
                }
            }

            product.Name = productDto.Name;
            product.Brand = productDto.Brand;
            product.Category = productDto.Category;
            product.Price = productDto.Price;
            product.Description = productDto.Description;
            product.StockQuantity = productDto.StockQuantity;
            product.IsActive = productDto.IsActive;
            product.ImageFileName = newFileName;

            await context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Product updated successfully!";

            if (User.IsInRole(UserRoles.ProductOwner) && !User.IsInRole(UserRoles.Admin))
            {
                return RedirectToAction("Index", "Seller");
            }

            return RedirectToAction("Index", "Products");
        }

        // Delete: Admin or Owner only
        [Authorize(Roles = $"{UserRoles.Admin},{UserRoles.ProductOwner}")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var product = await context.Products.FindAsync(id);
            if (product == null)
            {
                return RedirectToAction("Index", "Products");
            }

            var userId = GetCurrentUserId();
            if (!User.IsInRole(UserRoles.Admin) && product.OwnerId != userId)
            {
                return Forbid();
            }

            if (!string.IsNullOrEmpty(product.ImageFileName))
            {
                string imageFullPath = Path.Combine(environment.WebRootPath, "ProductImages", product.ImageFileName);
                if (System.IO.File.Exists(imageFullPath))
                {
                    System.IO.File.Delete(imageFullPath);
                }
            }

            context.Products.Remove(product);
            await context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Product deleted successfully.";

            if (User.IsInRole(UserRoles.ProductOwner) && !User.IsInRole(UserRoles.Admin))
            {
                return RedirectToAction("Index", "Seller");
            }

            return RedirectToAction("Index", "Products");
        }

        private int GetCurrentUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(claim, out int id) ? id : 0;
        }
    }
}
