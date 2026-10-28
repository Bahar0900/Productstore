using Microsoft.EntityFrameworkCore;

namespace ProductStore.Models
{
    [Keyless]
    public class OrderSummaryView
    {
        public int OrderId { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public DateTime OrderDate { get; set; }

        [Precision(10, 2)]
        public decimal TotalAmount { get; set; }

        public string OrderStatus { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerEmail { get; set; } = string.Empty;
        public string? PaymentMethod { get; set; }
        public string? PaymentStatus { get; set; }
        public string? TransactionId { get; set; }
        public int TotalItems { get; set; }
    }
}
