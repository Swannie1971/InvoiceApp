using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using System;
using InvoiceApp.Data;
using InvoiceApp.Models;
using Microsoft.EntityFrameworkCore;

namespace InvoiceApp.Services
{
    public interface IInvoiceService
    {
        Task<List<Invoice>> GetAllInvoicesAsync();
        Task<List<Invoice>> GetInvoicesByClientAsync(int clientId);
        Task<List<Invoice>> GetInvoicesByStatusAsync(InvoiceStatus status);
        Task<List<Invoice>> GetInvoicesByDateRangeAsync(DateTime startDate, DateTime endDate);
        Task<Invoice?> GetInvoiceByIdAsync(int id);
        Task<Invoice?> GetInvoiceByNumberAsync(string invoiceNumber);
        Task<Invoice> CreateInvoiceAsync(Invoice invoice);
        Task<Invoice> UpdateInvoiceAsync(Invoice invoice);
        Task DeleteInvoiceAsync(int id);
        Task<Invoice> DuplicateInvoiceAsync(int id);
        Task<string> GenerateInvoiceNumberAsync();
        Task UpdateInvoiceStatusAsync(int id, InvoiceStatus status);
        Task<List<Invoice>> GetOverdueInvoicesAsync();
    }

    public class InvoiceService : IInvoiceService
    {
        private readonly InvoiceDbContext _context;
        private readonly ISettingsService _settingsService;

        public InvoiceService(InvoiceDbContext context, ISettingsService settingsService)
        {
            _context = context;
            _settingsService = settingsService;
        }

        public async Task<List<Invoice>> GetAllInvoicesAsync()
        {
            return await _context.Invoices
                .Include(i => i.Client)
                .Include(i => i.LineItems)
                    .ThenInclude(li => li.Product)
                .Include(i => i.Payments)
                .OrderByDescending(i => i.InvoiceDate)
                .ToListAsync();
        }

        public async Task<List<Invoice>> GetInvoicesByClientAsync(int clientId)
        {
            return await _context.Invoices
                .Include(i => i.Client)
                .Include(i => i.LineItems)
                .Where(i => i.ClientId == clientId)
                .OrderByDescending(i => i.InvoiceDate)
                .ToListAsync();
        }

        public async Task<List<Invoice>> GetInvoicesByStatusAsync(InvoiceStatus status)
        {
            return await _context.Invoices
                .Include(i => i.Client)
                .Include(i => i.LineItems)
                .Where(i => i.Status == status)
                .OrderByDescending(i => i.InvoiceDate)
                .ToListAsync();
        }

        public async Task<List<Invoice>> GetInvoicesByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            return await _context.Invoices
                .Include(i => i.Client)
                .Include(i => i.LineItems)
                .Where(i => i.InvoiceDate >= startDate && i.InvoiceDate <= endDate)
                .OrderByDescending(i => i.InvoiceDate)
                .ToListAsync();
        }

        public async Task<Invoice?> GetInvoiceByIdAsync(int id)
        {
            return await _context.Invoices
                .Include(i => i.Client)
                .Include(i => i.LineItems)
                    .ThenInclude(li => li.Product)
                .Include(i => i.Payments)
                .FirstOrDefaultAsync(i => i.Id == id);
        }

        public async Task<Invoice?> GetInvoiceByNumberAsync(string invoiceNumber)
        {
            return await _context.Invoices
                .Include(i => i.Client)
                .Include(i => i.LineItems)
                .FirstOrDefaultAsync(i => i.InvoiceNumber == invoiceNumber);
        }

        public async Task<Invoice> CreateInvoiceAsync(Invoice invoice)
        {
            if (string.IsNullOrWhiteSpace(invoice.InvoiceNumber))
            {
                invoice.InvoiceNumber = await GenerateInvoiceNumberAsync();
            }

            CalculateInvoiceTotals(invoice);
            invoice.CreatedAt = DateTime.Now;

            _context.Invoices.Add(invoice);
            await _context.SaveChangesAsync();

            return invoice;
        }

        public async Task<Invoice> UpdateInvoiceAsync(Invoice invoice)
        {
            CalculateInvoiceTotals(invoice);
            invoice.UpdatedAt = DateTime.Now;

            _context.Invoices.Update(invoice);
            await _context.SaveChangesAsync();

            return invoice;
        }

        public async Task DeleteInvoiceAsync(int id)
        {
            var invoice = await _context.Invoices
                .Include(i => i.LineItems)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (invoice != null)
            {
                _context.Invoices.Remove(invoice);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<Invoice> DuplicateInvoiceAsync(int id)
        {
            var original = await GetInvoiceByIdAsync(id);
            if (original == null)
                throw new InvalidOperationException("Invoice not found");

            var duplicate = new Invoice
            {
                InvoiceNumber = await GenerateInvoiceNumberAsync(),
                ClientId = original.ClientId,
                InvoiceDate = DateTime.Now,
                DueDate = DateTime.Now.AddDays(30),
                Notes = original.Notes,
                PaymentTerms = original.PaymentTerms,
                Status = InvoiceStatus.Draft,
                LineItems = original.LineItems.Select(li => new InvoiceLineItem
                {
                    ProductId = li.ProductId,
                    Description = li.Description,
                    Quantity = li.Quantity,
                    UnitPrice = li.UnitPrice,
                    TaxRate = li.TaxRate,
                    SortOrder = li.SortOrder
                }).ToList()
            };

            return await CreateInvoiceAsync(duplicate);
        }

        public async Task<string> GenerateInvoiceNumberAsync()
        {
            var settings = await _settingsService.GetSettingsAsync();
            var invoiceNumber = $"{settings.InvoicePrefix}{settings.InvoiceNextNumber:D4}";

            // Increment the counter
            settings.InvoiceNextNumber++;
            await _settingsService.UpdateSettingsAsync(settings);

            return invoiceNumber;
        }

        public async Task UpdateInvoiceStatusAsync(int id, InvoiceStatus status)
        {
            var invoice = await _context.Invoices.FindAsync(id);
            if (invoice != null)
            {
                invoice.Status = status;
                invoice.UpdatedAt = DateTime.Now;

                if (status == InvoiceStatus.Sent && !invoice.SentDate.HasValue)
                {
                    invoice.SentDate = DateTime.Now;
                }
                else if (status == InvoiceStatus.Paid && !invoice.PaidDate.HasValue)
                {
                    invoice.PaidDate = DateTime.Now;
                }

                await _context.SaveChangesAsync();
            }
        }

        public async Task<List<Invoice>> GetOverdueInvoicesAsync()
        {
            var today = DateTime.Today;
            return await _context.Invoices
                .Include(i => i.Client)
                .Include(i => i.LineItems)
                .Where(i => i.Status == InvoiceStatus.Sent && i.DueDate < today)
                .OrderBy(i => i.DueDate)
                .ToListAsync();
        }

        private void CalculateInvoiceTotals(Invoice invoice)
        {
            decimal subtotal = 0;
            decimal taxAmount = 0;

            foreach (var item in invoice.LineItems)
            {
                var lineSubtotal = item.Quantity * item.UnitPrice;
                var lineTax = lineSubtotal * (item.TaxRate / 100);
                item.LineTotal = lineSubtotal + lineTax;

                subtotal += lineSubtotal;
                taxAmount += lineTax;
            }

            invoice.Subtotal = subtotal;
            invoice.TaxAmount = taxAmount;
            invoice.Total = subtotal + taxAmount;
        }
    }
}


