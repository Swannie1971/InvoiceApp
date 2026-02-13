using InvoiceApp.Models;
using QuestPDF;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace InvoiceApp.Services
{
    public interface IPdfService
    {
        Task<byte[]> GenerateInvoicePdfAsync(Invoice invoice, AppSettings settings);
        Task<byte[]> GenerateStatementPdfAsync(Statement statement, AppSettings settings);
        Task SavePdfAsync(byte[] pdfData, string filePath);
    }

    public class PdfService : IPdfService
    {
        public PdfService()
        {
            // Set QuestPDF license (Community license is free for commercial use)
            QuestPDF.Settings.License = LicenseType.Community;
        }

        public async Task<byte[]> GenerateInvoicePdfAsync(Invoice invoice, AppSettings settings)
        {
            return await Task.Run(() =>
            {
                var document = Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Size(PageSizes.A4);
                        page.Margin(50);
                        page.DefaultTextStyle(x => x.FontSize(10));

                        page.Header().Element(c => ComposeInvoiceHeader(c, invoice, settings));
                        page.Content().Element(c => ComposeInvoiceContent(c, invoice, settings));
                        page.Footer().Element(c => ComposeFooter(c, settings));
                    });
                });

                return document.GeneratePdf();
            });
        }

        public async Task<byte[]> GenerateStatementPdfAsync(Statement statement, AppSettings settings)
        {
            return await Task.Run(() =>
            {
                var document = Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Size(PageSizes.A4);
                        page.Margin(50);
                        page.DefaultTextStyle(x => x.FontSize(10));

                        page.Header().Element(c => ComposeStatementHeader(c, statement, settings));
                        page.Content().Element(c => ComposeStatementContent(c, statement, settings));
                        page.Footer().Element(c => ComposeFooter(c, settings));
                    });
                });

                return document.GeneratePdf();
            });
        }

        public async Task SavePdfAsync(byte[] pdfData, string filePath)
        {
            await File.WriteAllBytesAsync(filePath, pdfData);
        }

        private void ComposeInvoiceHeader(IContainer container, Invoice invoice, AppSettings settings)
        {
            container.Column(column =>
            {
                column.Item().Row(row =>
                {
                    row.RelativeItem().Column(col =>
                    {
                        // Company logo
                        if (settings.CompanyLogo != null && settings.CompanyLogo.Length > 0)
                        {
                            col.Item().MaxHeight(60).Image(settings.CompanyLogo);
                        }

                        // Company details
                        col.Item().PaddingTop(10).Text(settings.CompanyName).FontSize(16).Bold();
                        col.Item().Text(settings.CompanyAddress.Replace("\n", ", "));
                        col.Item().Text($"Phone: {settings.CompanyPhone}");
                        col.Item().Text($"Email: {settings.CompanyEmail}");
                        if (!string.IsNullOrWhiteSpace(settings.CompanyVatNumber))
                        {
                            col.Item().Text($"Tax: {settings.CompanyVatNumber}");
                        }
                    });

                    row.RelativeItem().Column(col =>
                    {
                        col.Item().AlignRight().Text("INVOICE").FontSize(24).Bold();
                        col.Item().AlignRight().PaddingTop(10).Text($"Invoice #: {invoice.InvoiceNumber}").Bold();
                        col.Item().AlignRight().Text($"Date: {invoice.InvoiceDate:MMM dd, yyyy}");
                        col.Item().AlignRight().Text($"Due Date: {invoice.DueDate:MMM dd, yyyy}");
                        col.Item().AlignRight().Text($"Status: {invoice.Status}").Bold();
                    });
                });

                // Bill to section
                column.Item().PaddingTop(30).Row(row =>
                {
                    row.RelativeItem().Column(col =>
                    {
                        col.Item().Text("Bill To:").Bold();
                        col.Item().Text(invoice.Client.CompanyName).Bold();
                        if (!string.IsNullOrWhiteSpace(invoice.Client.ContactPerson))
                        {
                            col.Item().Text($"Attn: {invoice.Client.ContactPerson}");
                        }
                        if (!string.IsNullOrWhiteSpace(invoice.Client.BillingAddress))
                        {
                            col.Item().Text(invoice.Client.BillingAddress);
                        }
                        if (!string.IsNullOrWhiteSpace(invoice.Client.Email))
                        {
                            col.Item().Text($"Email: {invoice.Client.Email}");
                        }
                        if (!string.IsNullOrWhiteSpace(invoice.Client.VatNumber))
                        {
                            col.Item().Text($"Tax: {invoice.Client.VatNumber}");
                        }
                    });
                });
            });
        }

        private void ComposeInvoiceContent(IContainer container, Invoice invoice, AppSettings settings)
        {
            container.PaddingTop(20).Column(column =>
            {
                // Line items table
                column.Item().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(3);
                        columns.RelativeColumn(1);
                        columns.RelativeColumn(1.5f);
                        columns.RelativeColumn(1);
                        columns.RelativeColumn(1.5f);
                    });

                    // Header
                    table.Header(header =>
                    {
                        header.Cell().Element(CellStyle).Text("Description").Bold();
                        header.Cell().Element(CellStyle).AlignRight().Text("Qty").Bold();
                        header.Cell().Element(CellStyle).AlignRight().Text("Unit Price").Bold();
                        header.Cell().Element(CellStyle).AlignRight().Text("Tax %").Bold();
                        header.Cell().Element(CellStyle).AlignRight().Text("Total").Bold();

                        static IContainer CellStyle(IContainer c) => c.BorderBottom(1).BorderColor(Colors.Grey.Lighten1).PaddingVertical(5);
                    });

                    // Line items
                    foreach (var item in invoice.LineItems.OrderBy(li => li.SortOrder))
                    {
                        table.Cell().Element(CellStyle).Text(item.Description);
                        table.Cell().Element(CellStyle).AlignRight().Text(item.Quantity.ToString("N2"));
                        table.Cell().Element(CellStyle).AlignRight().Text($"{settings.CurrencySymbol}{item.UnitPrice:N2}");
                        table.Cell().Element(CellStyle).AlignRight().Text($"{item.TaxRate:N2}%");
                        table.Cell().Element(CellStyle).AlignRight().Text($"{settings.CurrencySymbol}{item.LineTotal:N2}");

                        static IContainer CellStyle(IContainer c) => c.BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingVertical(5);
                    }
                });

                // Totals
                column.Item().PaddingTop(20).AlignRight().Column(col =>
                {
                    col.Item().Row(row =>
                    {
                        row.AutoItem().Width(100).Text("Subtotal:");
                        row.AutoItem().Width(100).AlignRight().Text($"{settings.CurrencySymbol}{invoice.Subtotal:N2}");
                    });

                    col.Item().Row(row =>
                    {
                        row.AutoItem().Width(100).Text("Tax:");
                        row.AutoItem().Width(100).AlignRight().Text($"{settings.CurrencySymbol}{invoice.TaxAmount:N2}");
                    });

                    col.Item().PaddingTop(5).BorderTop(1).Row(row =>
                    {
                        row.AutoItem().Width(100).Text("Total:").FontSize(12).Bold();
                        row.AutoItem().Width(100).AlignRight().Text($"{settings.CurrencySymbol}{invoice.Total:N2}").FontSize(12).Bold();
                    });

                    // Amount due (if not paid)
                    if (invoice.Status != InvoiceStatus.Paid)
                    {
                        col.Item().PaddingTop(5).Row(row =>
                        {
                            row.AutoItem().Width(100).Text("Amount Due:").FontSize(12).Bold();
                            row.AutoItem().Width(100).AlignRight().Text($"{settings.CurrencySymbol}{invoice.Total:N2}").FontSize(12).Bold();
                        });
                    }
                });

                // Notes and payment terms
                if (!string.IsNullOrWhiteSpace(invoice.Notes) || !string.IsNullOrWhiteSpace(invoice.PaymentTerms))
                {
                    column.Item().PaddingTop(30).Column(col =>
                    {
                        if (!string.IsNullOrWhiteSpace(invoice.Notes))
                        {
                            col.Item().Text("Notes:").Bold();
                            col.Item().Text(invoice.Notes);
                            col.Item().PaddingTop(10);
                        }

                        if (!string.IsNullOrWhiteSpace(invoice.PaymentTerms))
                        {
                            col.Item().Text("Payment Terms:").Bold();
                            col.Item().Text(invoice.PaymentTerms);
                        }
                    });
                }
            });
        }

        private void ComposeStatementHeader(IContainer container, Statement statement, AppSettings settings)
        {
            container.Column(column =>
            {
                column.Item().Row(row =>
                {
                    row.RelativeItem().Column(col =>
                    {
                        if (settings.CompanyLogo != null && settings.CompanyLogo.Length > 0)
                        {
                            col.Item().MaxHeight(60).Image(settings.CompanyLogo);
                        }

                        col.Item().PaddingTop(10).Text(settings.CompanyName).FontSize(16).Bold();
                        col.Item().Text(settings.CompanyAddress.Replace("\n", ", "));
                        col.Item().Text($"Phone: {settings.CompanyPhone}");
                        col.Item().Text($"Email: {settings.CompanyEmail}");
                    });

                    row.RelativeItem().Column(col =>
                    {
                        col.Item().AlignRight().Text("STATEMENT").FontSize(24).Bold();
                        col.Item().AlignRight().PaddingTop(10).Text($"Period: {statement.StartDate:MMM dd, yyyy} - {statement.EndDate:MMM dd, yyyy}");
                        col.Item().AlignRight().Text($"Generated: {statement.CreatedAt:MMM dd, yyyy}");
                    });
                });

                column.Item().PaddingTop(30).Column(col =>
                {
                    col.Item().Text("For:").Bold();
                    col.Item().Text(statement.Client.CompanyName).Bold();
                    if (!string.IsNullOrWhiteSpace(statement.Client.BillingAddress))
                    {
                        col.Item().Text(statement.Client.BillingAddress);
                    }
                });
            });
        }

        private void ComposeStatementContent(IContainer container, Statement statement, AppSettings settings)
        {
            container.PaddingTop(20).Column(column =>
            {
                // Opening balance
                column.Item().PaddingBottom(10).Row(row =>
                {
                    row.RelativeItem().Text("Opening Balance:").Bold();
                    row.AutoItem().Text($"{settings.CurrencySymbol}{statement.OpeningBalance:N2}").Bold();
                });

                // Transaction table
                column.Item().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(2);
                        columns.RelativeColumn(3);
                        columns.RelativeColumn(1.5f);
                        columns.RelativeColumn(1.5f);
                        columns.RelativeColumn(1.5f);
                    });

                    table.Header(header =>
                    {
                        header.Cell().Element(CellStyle).Text("Date").Bold();
                        header.Cell().Element(CellStyle).Text("Description").Bold();
                        header.Cell().Element(CellStyle).AlignRight().Text("Debit").Bold();
                        header.Cell().Element(CellStyle).AlignRight().Text("Credit").Bold();
                        header.Cell().Element(CellStyle).AlignRight().Text("Balance").Bold();

                        static IContainer CellStyle(IContainer c) => c.BorderBottom(1).BorderColor(Colors.Grey.Lighten1).PaddingVertical(5);
                    });

                    foreach (var item in statement.LineItems.OrderBy(li => li.SortOrder))
                    {
                        table.Cell().Element(CellStyle).Text(item.Date.ToString("MMM dd, yyyy"));
                        table.Cell().Element(CellStyle).Text(item.Description);
                        table.Cell().Element(CellStyle).AlignRight().Text(item.Debit > 0 ? $"{settings.CurrencySymbol}{item.Debit:N2}" : "-");
                        table.Cell().Element(CellStyle).AlignRight().Text(item.Credit > 0 ? $"{settings.CurrencySymbol}{item.Credit:N2}" : "-");
                        table.Cell().Element(CellStyle).AlignRight().Text($"{settings.CurrencySymbol}{item.Balance:N2}");

                        static IContainer CellStyle(IContainer c) => c.BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingVertical(5);
                    }
                });

                // Closing balance
                column.Item().PaddingTop(20).AlignRight().Column(col =>
                {
                    col.Item().BorderTop(1).PaddingTop(5).Row(row =>
                    {
                        row.AutoItem().Width(120).Text("Closing Balance:").FontSize(12).Bold();
                        row.AutoItem().Width(120).AlignRight().Text($"{settings.CurrencySymbol}{statement.ClosingBalance:N2}").FontSize(12).Bold();
                    });

                    if (statement.ClosingBalance > 0)
                    {
                        col.Item().PaddingTop(5).Row(row =>
                        {
                            row.AutoItem().Width(120).Text("Amount Outstanding:").FontSize(12).Bold();
                            row.AutoItem().Width(120).AlignRight().Text($"{settings.CurrencySymbol}{statement.ClosingBalance:N2}").FontSize(12).Bold();
                        });
                    }
                });

                if (!string.IsNullOrWhiteSpace(statement.Notes))
                {
                    column.Item().PaddingTop(30).Column(col =>
                    {
                        col.Item().Text("Notes:").Bold();
                        col.Item().Text(statement.Notes);
                    });
                }
            });
        }

        private void ComposeFooter(IContainer container, AppSettings settings)
        {
            container.AlignCenter().Column(column =>
            {
                if (!string.IsNullOrWhiteSpace(settings.InvoiceFooter))
                {
                    column.Item().BorderTop(1).BorderColor(Colors.Grey.Lighten1).PaddingTop(10);
                    column.Item().Text(settings.InvoiceFooter).FontSize(9);
                }
            });
        }
    }
}



