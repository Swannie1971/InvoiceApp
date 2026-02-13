using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InvoiceApp.Models
{
    public class Statement
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ClientId { get; set; }

        [ForeignKey(nameof(ClientId))]
        public virtual Client Client { get; set; } = null!;

        [Required]
        public DateTime StartDate { get; set; }

        [Required]
        public DateTime EndDate { get; set; }

        public decimal OpeningBalance { get; set; }
        public decimal ClosingBalance { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [MaxLength(1000)]
        public string? Notes { get; set; }

        // Navigation properties
        public virtual ICollection<StatementLineItem> LineItems { get; set; } = new List<StatementLineItem>();
        public DateTime StatementDate { get; internal set; }
    }

    public class StatementLineItem
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int StatementId { get; set; }

        [ForeignKey(nameof(StatementId))]
        public virtual Statement Statement { get; set; } = null!;

        public int? InvoiceId { get; set; }

        [ForeignKey(nameof(InvoiceId))]
        public virtual Invoice? Invoice { get; set; }

        [Required]
        public DateTime Date { get; set; }

        [Required]
        [MaxLength(200)]
        public string Description { get; set; } = string.Empty;

        public decimal Debit { get; set; }
        public decimal Credit { get; set; }
        public decimal Balance { get; set; }

        public int SortOrder { get; set; }
    }
}


