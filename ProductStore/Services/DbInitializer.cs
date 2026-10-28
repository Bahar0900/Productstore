using Microsoft.EntityFrameworkCore;
using ProductStore.Models;

namespace ProductStore.Services
{
    public static class DbInitializer
    {
        public static async Task InitializeAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
            var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

            // Ensure database is created and migrations are applied
            await context.Database.MigrateAsync();

            // 1. Create SQL Database View if it does not exist
            var sqlCreateView = @"
                IF NOT EXISTS (SELECT * FROM sys.views WHERE object_id = OBJECT_ID(N'[dbo].[vw_OrderSummaries]'))
                BEGIN
                    EXEC(N'
                        CREATE VIEW [dbo].[vw_OrderSummaries] AS
                        SELECT 
                            o.Id AS OrderId,
                            o.OrderNumber,
                            o.OrderDate,
                            o.TotalAmount,
                            o.OrderStatus,
                            u.FullName AS CustomerName,
                            u.Email AS CustomerEmail,
                            p.PaymentMethod,
                            p.Status AS PaymentStatus,
                            p.TransactionId,
                            ISNULL((SELECT SUM(oi.Quantity) FROM OrderItems oi WHERE oi.OrderId = o.Id), 0) AS TotalItems
                        FROM Orders o
                        INNER JOIN Users u ON o.UserId = u.Id
                        LEFT JOIN Payments p ON p.OrderId = o.Id
                    ')
                END";
            await context.Database.ExecuteSqlRawAsync(sqlCreateView);

            // 2. Seed Default Users if none exist
            if (!await context.Users.AnyAsync(u => u.Role == UserRoles.Admin))
            {
                var adminUser = new User
                {
                    FullName = "Master Admin",
                    Email = "admin@productstore.com",
                    PasswordHash = passwordHasher.HashPassword("Admin@123"),
                    Role = UserRoles.Admin,
                    PhoneNumber = "01700000001",
                    Address = "Corporate Headquarters, Tech Park, Suite 400",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                var sellerUser = new User
                {
                    FullName = "Official Store Seller",
                    Email = "seller@productstore.com",
                    PasswordHash = passwordHasher.HashPassword("Seller@123"),
                    Role = UserRoles.ProductOwner,
                    PhoneNumber = "01700000002",
                    Address = "Merchant Hub, Industrial Zone",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                var customerUser = new User
                {
                    FullName = "John Customer",
                    Email = "customer@productstore.com",
                    PasswordHash = passwordHasher.HashPassword("Customer@123"),
                    Role = UserRoles.Customer,
                    PhoneNumber = "01700000003",
                    Address = "House 12, Road 4, Sector 7, Dhaka",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                context.Users.AddRange(adminUser, sellerUser, customerUser);
                await context.SaveChangesAsync();

                // 3. Associate any existing unassigned products with the seeded Seller
                var unassignedProducts = await context.Products.Where(p => p.OwnerId == null).ToListAsync();
                foreach (var product in unassignedProducts)
                {
                    product.OwnerId = sellerUser.Id;
                    product.StockQuantity = 25;
                    product.IsActive = true;
                }
                await context.SaveChangesAsync();
            }
        }
    }
}
