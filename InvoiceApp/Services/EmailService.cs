using System;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using System.Windows;
using InvoiceApp.Data;
using InvoiceApp.Models;

namespace InvoiceApp.Services
{
    public class EmailService : IEmailService
    {
        private readonly InvoiceDbContext _context;
        private readonly ISettingsService _settingsService;

        public EmailService(InvoiceDbContext context, ISettingsService settingsService)
        {
            _context = context;
            _settingsService = settingsService;
        }

        public async Task<bool> SendInvoiceEmailAsync(Invoice invoice, AppSettings settings)
        {
            try
            {
                using var client = CreateSmtpClient(settings);
                await Task.Run(() => client.Send(CreateInvoiceMessage(invoice, settings)));
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> TestEmailConnectionAsync(AppSettings settings)
        {
            // DEBUG: Show what credentials we're using
            MessageBox.Show($"Testing with:\nServer: {settings.SmtpServer}\nPort: {settings.SmtpPort}\nUsername: {settings.SmtpUsername}\nPassword length: {settings.SmtpPassword?.Length ?? 0} chars\nSSL: {settings.SmtpUseSsl}",
                "Debug Info", MessageBoxButton.OK, MessageBoxImage.Information);

            try
            {
                using var client = CreateSmtpClient(settings);
                await Task.Run(() => client.Send(CreateTestMessage(settings)));
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> SendInvoiceEmailWithAttachmentsAsync(
            Invoice invoice,
            string toEmail,
            string subject,
            string body,
            string[] attachmentPaths)
        {
            try
            {
                var settings = await _settingsService.GetSettingsAsync();

                using var client = CreateSmtpClient(settings);
                using var message = new MailMessage();

                // From
                message.From = new MailAddress(
                    settings.EmailFromAddress ?? settings.CompanyEmail,
                    settings.EmailFromName ?? settings.CompanyName);

                // To
                message.To.Add(toEmail);

                // Subject and Body
                message.Subject = subject;
                message.Body = body;
                message.IsBodyHtml = false;

                // Add attachments
                if (attachmentPaths != null && attachmentPaths.Length > 0)
                {
                    foreach (var path in attachmentPaths)
                    {
                        if (File.Exists(path))
                        {
                            var attachment = new Attachment(path);
                            message.Attachments.Add(attachment);
                        }
                    }
                }

                // Send
                await Task.Run(() => client.Send(message));

                // Log success
                _context.EmailLogs.Add(new EmailLog
                {
                    RecipientEmail = toEmail,
                    Subject = subject,
                    Body = body,
                    SentAt = DateTime.Now,
                    Success = true,
                    InvoiceId = invoice.Id
                });
                await _context.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                // Log failure
                _context.EmailLogs.Add(new EmailLog
                {
                    RecipientEmail = toEmail,
                    Subject = subject,
                    Body = body,
                    SentAt = DateTime.Now,
                    Success = false,
                    ErrorMessage = ex.Message,
                    InvoiceId = invoice.Id
                });
                await _context.SaveChangesAsync();

                return false;
            }
        }

        private SmtpClient CreateSmtpClient(AppSettings settings)
        {
            var client = new SmtpClient(settings.SmtpServer, settings.SmtpPort)
            {
                EnableSsl = settings.SmtpUseSsl,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(settings.SmtpUsername, settings.SmtpPassword)
            };
            return client;
        }

        private MailMessage CreateTestMessage(AppSettings settings)
        {
            var message = new MailMessage
            {
                From = new MailAddress(settings.EmailFromAddress ?? settings.CompanyEmail, settings.EmailFromName ?? settings.CompanyName),
                Subject = "Test Email from Invoice App",
                Body = "This is a test email to verify your SMTP settings are configured correctly.\n\nIf you receive this message, your email configuration is working!",
                IsBodyHtml = false
            };

            message.To.Add(settings.EmailFromAddress ?? settings.CompanyEmail);
            return message;
        }

        private MailMessage CreateInvoiceMessage(Invoice invoice, AppSettings settings)
        {
            var message = new MailMessage
            {
                From = new MailAddress(settings.EmailFromAddress ?? settings.CompanyEmail, settings.EmailFromName ?? settings.CompanyName),
                Subject = $"Invoice {invoice.InvoiceNumber} from {settings.CompanyName}",
                Body = $"Dear {invoice.Client.CompanyName},\n\nPlease find attached invoice {invoice.InvoiceNumber}.\n\nThank you for your business.\n\nBest regards,\n{settings.CompanyName}",
                IsBodyHtml = false
            };

            if (!string.IsNullOrWhiteSpace(invoice.Client.Email))
            {
                message.To.Add(invoice.Client.Email);
            }

            return message;
        }
    }
}