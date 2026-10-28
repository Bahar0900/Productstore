namespace ProductStore.Models.ViewModels
{
    public class SellerDashboardViewModel
    {
        public int TotalProducts { get; set; }
        public int LowStockProducts { get; set; }
        public int TotalItemsSold { get; set; }
        public decimal TotalEarnings { get; set; }
        public List<Product> MyProducts { get; set; } = new List<Product>();
        public List<OrderItem> RecentSales { get; set; } = new List<OrderItem>();
    }
}
