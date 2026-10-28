using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProductStore.Models
{
    public static class OrderStatuses
    {
        public const string Pending = "Pending";
        public const string Paid = "Paid";
        public const string Processing = "Processing";
        public const string Shipped = "Shipped";
        public const string Delivered = "Delivered";
        public const string Cancelled = "Cancelled";
    }

    public class Order
    {
        public int Id { get; set; }

        [Required, MaxLength(50)]
        public string OrderNumber { get; set; } = string.Empty;

        public int UserId { get; set; }

        [ForeignKey("UserId")]
        public User User { get; set; } = null!;

        public DateTime OrderDate { get; set; } = DateTime.UtcNow;

        [Precision(10, 2)]
        public decimal SubTotal { get; set; }

        [Precision(10, 2)]
        public decimal ShippingFee { get; set; } = 50.00m;

        [Precision(10, 2)]
        public decimal TotalAmount { get; set; }

        [Required, MaxLength(30)]
        public string OrderStatus { get; set; } = OrderStatuses.Pending;

        [Required, MaxLength(100)]
        public string ShippingFullName { get; set; } = string.Empty;

        [Required, EmailAddress, MaxLength(150)]
        public string ShippingEmail { get; set; } = string.Empty;

        [Required, MaxLength(30)]
        public string ShippingPhone { get; set; } = string.Empty;

        [Required, MaxLength(300)]
        public string ShippingAddress { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? OrderNotes { get; set; }

        public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

        public Payment? Payment { get; set; }
    }
}
