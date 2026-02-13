using System.Threading.Tasks;
using InvoiceApp.Models;

namespace InvoiceApp.Services
{
    public interface IEmailService
    {
        Task<bool> SendInvoiceEmailAsync(Invoice invoice, AppSettings settings);
        Task<bool> TestEmailConnectionAsync(AppSettings settings);
        Task<bool> SendInvoiceEmailWithAttachmentsAsync(Invoice invoice, string toEmail, string subject, string body, string[] attachmentPaths);
    }
}