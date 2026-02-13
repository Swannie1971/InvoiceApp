using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using System;
using InvoiceApp.Data;
using InvoiceApp.Models;
using Microsoft.EntityFrameworkCore;

namespace InvoiceApp.Services
{
    public interface ISettingsService
    {
        Task<AppSettings> GetSettingsAsync();
        Task<AppSettings> UpdateSettingsAsync(AppSettings settings);
    }

    public class SettingsService : ISettingsService
    {
        private readonly InvoiceDbContext _context;

        public SettingsService(InvoiceDbContext context)
        {
            _context = context;
        }

        public async Task<AppSettings> GetSettingsAsync()
        {
            var settings = await _context.AppSettings.FirstOrDefaultAsync();
            
            if (settings == null)
            {
                // Create default settings if none exist
                settings = new AppSettings
                {
                    CompanyName = "Your Company Name",
                    CompanyAddress = "123 Business Street\nCity, State 12345",
                    CompanyPhone = "(555) 123-4567",
                    CompanyEmail = "info@yourcompany.com",
                    InvoicePrefix = "INV",
                    InvoiceNextNumber = 1001,
                    DefaultTaxRate = 0,
                    DefaultPaymentTerms = "Payment due within 30 days",
                    InvoiceFooter = "Thank you for your business!",
                    SmtpPort = 587,
                    SmtpUseSsl = true,
                    DefaultEmailSubject = "Invoice #{InvoiceNumber} from {CompanyName}",
                    DefaultEmailBody = "Dear {ClientName},\n\nPlease find attached invoice #{InvoiceNumber}.\n\nThank you for your business.\n\nBest regards,\n{CompanyName}"
                };

                _context.AppSettings.Add(settings);
                await _context.SaveChangesAsync();
            }

            return settings;
        }

        public async Task<AppSettings> UpdateSettingsAsync(AppSettings settings)
        {
            settings.UpdatedAt = DateTime.Now;
            _context.AppSettings.Update(settings);
            await _context.SaveChangesAsync();
            return settings;
        }
    }
}


