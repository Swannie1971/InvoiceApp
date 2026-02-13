using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InvoiceApp.Models
{
    public class Payment
    {
        public int Id { get; set; }

        [Required]
        public int InvoiceId { get; set; }

        [ForeignKey(nameof(InvoiceId))]
        public virtual Invoice Invoice { get; set; } = null!;

        [Required]
        public decimal Amount { get; set; }

        [Required]
        public DateTime PaymentDate { get; set; } = DateTime.Today;

        [MaxLength(50)]
        public string PaymentMethod { get; set; } = "Cash"; // Cash, Bank Transfer, Credit Card, Check, Other

        [MaxLength(100)]
        public string? ReferenceNumber { get; set; } // Check number, transaction ID, etc.

        [MaxLength(500)]
        public string? Notes { get; set; }

        public int? RecordedByUserId { get; set; }

        [ForeignKey(nameof(RecordedByUserId))]
        public virtual User? RecordedBy { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}