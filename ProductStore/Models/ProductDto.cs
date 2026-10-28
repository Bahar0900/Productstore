using System.ComponentModel.DataAnnotations;

namespace ProductStore.Models
{
    public class ProductDto
    {
        [Required, MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        public string Brand { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        public string Category { get; set; } = string.Empty;

        [Required, Range(0.01, 1000000.00, ErrorMessage = "Price must be greater than 0")]
        public decimal Price { get; set; } = 0;

        [Required]
        public string Description { get; set; } = string.Empty;

        [Range(0, 100000, ErrorMessage = "Stock must be 0 or greater")]
        public int StockQuantity { get; set; } = 20;

        public bool IsActive { get; set; } = true;

        public IFormFile? ImageFile { get; set; }
    }
}
