using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InvoiceApp.Models
{
    public class Invoice
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string InvoiceNumber { get; set; } = string.Empty;

        [Required]
        public int ClientId { get; set; }

        [ForeignKey(nameof(ClientId))]
        public virtual Client Client { get; set; } = null!;

        [Required]
        public DateTime InvoiceDate { get; set; } = DateTime.Now;

        [Required]
        public DateTime DueDate { get; set; } = DateTime.Now.AddDays(30);

        [MaxLength(1000)]
        public string? Notes { get; set; }

        [MaxLength(500)]
        public string? PaymentTerms { get; set; }
        [NotMapped]
        public decimal AmountPaid => Payments?.Sum(p => p.Amount) ?? 0;

        [NotMapped]
        public decimal AmountRemaining => Total - AmountPaid;

        [NotMapped]
        public bool IsFullyPaid => AmountRemaining <= 0;

        [NotMapped]
        public bool IsPartiallyPaid => AmountPaid > 0 && AmountRemaining > 0;
        public decimal Subtotal { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal Total { get; set; }

        [Required]
        public InvoiceStatus Status { get; set; } = InvoiceStatus.Draft;

        public DateTime? SentDate { get; set; }
        public DateTime? PaidDate { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        public virtual ICollection<InvoiceLineItem> LineItems { get; set; } = new List<InvoiceLineItem>();
        public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();
    }

    public enum InvoiceStatus
    {
        Draft = 0,
        Sent = 1,
        PartiallyPaid = 2,
        Paid = 3,
        Overdue = 4
    }
}


