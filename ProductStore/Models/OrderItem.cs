using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProductStore.Models
{
    public class OrderItem
    {
        public int Id { get; set; }

        public int OrderId { get; set; }

        [ForeignKey("OrderId")]
        public Order Order { get; set; } = null!;

        public int ProductId { get; set; }

        [ForeignKey("ProductId")]
        public Product Product { get; set; } = null!;

        [Precision(10, 2)]
        public decimal UnitPrice { get; set; }

        public int Quantity { get; set; }

        [Precision(10, 2)]
        public decimal TotalPrice { get; set; }
    }
}
