namespace ProductStore.Models.ViewModels
{
    public class AdminDashboardViewModel
    {
        public decimal TotalRevenue { get; set; }
        public int TotalOrders { get; set; }
        public int TotalUsers { get; set; }
        public int TotalProducts { get; set; }
        public int PendingOrders { get; set; }
        public int PendingPayments { get; set; }
        public List<Order> RecentOrders { get; set; } = new List<Order>();
        public List<User> RecentUsers { get; set; } = new List<User>();
    }

    public class UserManagementViewModel
    {
        public List<User> Users { get; set; } = new List<User>();
    }
}
