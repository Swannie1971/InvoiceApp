using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using System;
using System.ComponentModel.DataAnnotations;

namespace InvoiceApp.Models
{
    public class AppSettings
    {
        [Key]
        public int Id { get; set; }

        // Company Profile
        [MaxLength(200)]
        public string CompanyName { get; set; } = string.Empty;

        [MaxLength(500)]
        public string CompanyAddress { get; set; } = string.Empty;

        [MaxLength(100)]
        public string CompanyPhone { get; set; } = string.Empty;

        [MaxLength(100)]
        public string CompanyEmail { get; set; } = string.Empty;

        [MaxLength(50)]
        public string CompanyVatNumber { get; set; } = string.Empty;

        [MaxLength(10)]
        public string CurrencySymbol { get; set; } = "R";

        [MaxLength(10)]
        public string CurrencyCode { get; set; } = "ZAR";

        public byte[]? CompanyLogo { get; set; }

        // Invoice Settings
        [MaxLength(10)]
        public string InvoicePrefix { get; set; } = "INV";

        public int InvoiceNextNumber { get; set; } = 1;

        public decimal DefaultTaxRate { get; set; } = 0;

        [MaxLength(500)]
        public string DefaultPaymentTerms { get; set; } = "Payment due within 30 days";

        [MaxLength(1000)]
        public string InvoiceFooter { get; set; } = string.Empty;

        // Email Settings
        [MaxLength(100)]
        public string SmtpServer { get; set; } = string.Empty;

        public int SmtpPort { get; set; } = 587;

        [MaxLength(100)]
        public string SmtpUsername { get; set; } = string.Empty;

        public string SmtpPassword { get; set; } = string.Empty;

        public bool SmtpUseSsl { get; set; } = true;

        [MaxLength(100)]
        public string EmailFromAddress { get; set; } = string.Empty;

        [MaxLength(100)]
        public string EmailFromName { get; set; } = string.Empty;

        [MaxLength(200)]
        public string DefaultEmailSubject { get; set; } = "Invoice #{InvoiceNumber} from {CompanyName}";

        [MaxLength(2000)]
        public string DefaultEmailBody { get; set; } = "Dear {ClientName},\n\nPlease find attached invoice #{InvoiceNumber}.\n\nThank you for your business.\n\nBest regards,\n{CompanyName}";

        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }
}


