using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using InvoiceApp.Models;
using InvoiceApp.Services;

namespace InvoiceApp.Views
{
    public partial class StatementEmailDialog : Window
    {
        private readonly IEmailService _emailService;
        private readonly IPdfService _pdfService;
        private readonly ISettingsService _settingsService;
        private readonly Statement _statement;
        private readonly byte[] _statementPdf;
        private string _tempPdfPath;

        public StatementEmailDialog(
            IEmailService emailService,
            IPdfService pdfService,
            ISettingsService settingsService,
            Statement statement,
            byte[] statementPdf,
            string defaultSubject,
            string defaultBody)
        {
            InitializeComponent();

            _emailService = emailService;
            _pdfService = pdfService;
            _settingsService = settingsService;
            _statement = statement;
            _statementPdf = statementPdf;

            // Set display info
            StatementInfoText.Text = $"Statement for {statement.Client.CompanyName} ({statement.StartDate:d} - {statement.EndDate:d})";

            // Set email fields
            ToEmailTextBox.Text = statement.Client.Email ?? string.Empty;
            SubjectTextBox.Text = defaultSubject;
            BodyTextBox.Text = defaultBody;

            // Set attachment info
            AttachmentText.Text = $"Statement_{statement.Client.CompanyName}_{statement.StartDate:yyyyMMdd}-{statement.EndDate:yyyyMMdd}.pdf";
        }

        private async void SendButton_Click(object sender, RoutedEventArgs e)
        {
            // Validate
            if (string.IsNullOrWhiteSpace(ToEmailTextBox.Text))
            {
                MessageBox.Show("Please enter a recipient email address.", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(SubjectTextBox.Text))
            {
                MessageBox.Show("Please enter an email subject.", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                // Save PDF to temp file
                _tempPdfPath = Path.Combine(Path.GetTempPath(),
                    $"Statement_{_statement.Client.CompanyName}_{DateTime.Now:yyyyMMddHHmmss}.pdf");
                await _pdfService.SavePdfAsync(_statementPdf, _tempPdfPath);

                // Get settings
                var settings = await _settingsService.GetSettingsAsync();

                // Send email
                var success = await SendEmailAsync(ToEmailTextBox.Text, SubjectTextBox.Text, BodyTextBox.Text, _tempPdfPath);

                // Clean up temp file
                try { File.Delete(_tempPdfPath); } catch { }

                if (success)
                {
                    MessageBox.Show("Statement sent successfully!", "Success",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    DialogResult = true;
                    Close();
                }
                else
                {
                    MessageBox.Show("Failed to send email. Please check your email settings.", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error sending email: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task<bool> SendEmailAsync(string toEmail, string subject, string body, string attachmentPath)
        {
            try
            {
                var settings = await _settingsService.GetSettingsAsync();

                using var message = new System.Net.Mail.MailMessage();
                message.From = new System.Net.Mail.MailAddress(
                    settings.EmailFromAddress ?? settings.CompanyEmail,
                    settings.EmailFromName ?? settings.CompanyName);
                message.To.Add(toEmail);
                message.Subject = subject;
                message.Body = body;
                message.IsBodyHtml = false;

                // Add attachment
                if (File.Exists(attachmentPath))
                {
                    var attachment = new System.Net.Mail.Attachment(attachmentPath);
                    message.Attachments.Add(attachment);
                }

                // Send via SMTP
                using var client = new System.Net.Mail.SmtpClient(settings.SmtpServer, settings.SmtpPort);
                client.EnableSsl = settings.SmtpUseSsl;
                client.UseDefaultCredentials = false;
                client.Credentials = new System.Net.NetworkCredential(settings.SmtpUsername, settings.SmtpPassword);

                await Task.Run(() => client.Send(message));
                return true;
            }
            catch
            {
                return false;
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}