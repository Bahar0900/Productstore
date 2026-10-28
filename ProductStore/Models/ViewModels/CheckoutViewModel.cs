using System.ComponentModel.DataAnnotations;

namespace ProductStore.Models.ViewModels
{
    public class CheckoutViewModel
    {
        // Shipping Information
        [Required(ErrorMessage = "Full Name is required"), MaxLength(100)]
        public string ShippingFullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required"), EmailAddress, MaxLength(150)]
        public string ShippingEmail { get; set; } = string.Empty;

        [Required(ErrorMessage = "Contact phone number is required"), Phone, MaxLength(30)]
        public string ShippingPhone { get; set; } = string.Empty;

        [Required(ErrorMessage = "Full delivery address is required"), MaxLength(300)]
        public string ShippingAddress { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? OrderNotes { get; set; }

        // Payment Information
        [Required(ErrorMessage = "Please select a payment method")]
        public string PaymentMethod { get; set; } = PaymentMethods.MobileBanking;

        // Mobile Banking
        public string? MobileProvider { get; set; } = "bKash"; // bKash, Nagad, Rocket
        public string? MobileAccountNumber { get; set; }
        public string? MobileTransactionId { get; set; }

        // Direct Banking
        public string? BankName { get; set; } = "City Bank";
        public string? BankAccountNumber { get; set; }
        public string? BankTransactionId { get; set; }

        // Order calculation preview
        public decimal SubTotal { get; set; }
        public decimal ShippingFee { get; set; } = 50.00m;
        public decimal TotalAmount { get; set; }
        public List<CartItem> Items { get; set; } = new List<CartItem>();
    }
}
