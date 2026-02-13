using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InvoiceApp.Models
{
    public class EmailLog
    {
        [Key]
        public int Id { get; set; }

        public int? InvoiceId { get; set; }

        [ForeignKey(nameof(InvoiceId))]
        public virtual Invoice? Invoice { get; set; }

        public int? StatementId { get; set; }

        [ForeignKey(nameof(StatementId))]
        public virtual Statement? Statement { get; set; }

        [Required]
        [MaxLength(200)]
        public string RecipientEmail { get; set; } = string.Empty;

        [Required]
        [MaxLength(300)]
        public string Subject { get; set; } = string.Empty;

        [Required]
        public string Body { get; set; } = string.Empty;

        public DateTime SentAt { get; set; } = DateTime.Now;

        public bool Success { get; set; }

        [MaxLength(1000)]
        public string? ErrorMessage { get; set; }
    }
}


