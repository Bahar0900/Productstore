using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProductStore.Models
{
    public static class PaymentMethods
    {
        public const string MobileBanking = "MobileBanking";
        public const string DirectBanking = "DirectBanking";
        public const string CashOnDelivery = "CashOnDelivery";
    }

    public static class PaymentStatuses
    {
        public const string Pending = "Pending";
        public const string Verified = "Verified";
        public const string Failed = "Failed";
        public const string Refunded = "Refunded";
    }

    public class Payment
    {
        public int Id { get; set; }

        public int OrderId { get; set; }

        [ForeignKey("OrderId")]
        public Order Order { get; set; } = null!;

        [Required, MaxLength(40)]
        public string PaymentMethod { get; set; } = PaymentMethods.MobileBanking;

        [MaxLength(60)]
        public string? Provider { get; set; } // e.g. bKash, Nagad, Rocket, City Bank, DBBL, Card

        [MaxLength(60)]
        public string? AccountNumber { get; set; } // Sender's mobile or bank account

        [MaxLength(100)]
        public string? TransactionId { get; set; } // TrxID or Bank Reference

        [Precision(10, 2)]
        public decimal Amount { get; set; }

        [Required, MaxLength(30)]
        public string Status { get; set; } = PaymentStatuses.Pending;

        public DateTime PaymentDate { get; set; } = DateTime.UtcNow;

        public DateTime? VerifiedAt { get; set; }

        [MaxLength(300)]
        public string? AdminNotes { get; set; }
    }
}
