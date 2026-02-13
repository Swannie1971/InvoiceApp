using System;
using System.IO;
using System.Linq;
using System.Windows;
using InvoiceApp.Models;
using InvoiceApp.Services;
using Microsoft.Win32;

namespace InvoiceApp.Views
{
    public partial class StatementViewDialog : Window
    {
        private readonly IStatementService _statementService;
        private readonly IPdfService _pdfService;
        private readonly IEmailService _emailService;
        private readonly ISettingsService _settingsService;
        private Statement _statement; // ← This was missing (Step 8)

        public StatementViewDialog(
            IStatementService statementService,
            IPdfService pdfService,
            IEmailService emailService,
            ISettingsService settingsService,
            int statementId)
        {
            InitializeComponent();

            _statementService = statementService;
            _pdfService = pdfService;
            _emailService = emailService;
            _settingsService = settingsService;

            LoadStatementAsync(statementId);
        }

        private async void LoadStatementAsync(int statementId)
        {
            _statement = await _statementService.GetStatementByIdAsync(statementId);

            if (_statement != null)
            {
                // Display statement header
                ClientNameText.Text = _statement.Client.CompanyName;
                PeriodText.Text = $"{_statement.StartDate:d} - {_statement.EndDate:d}";
                OpeningBalanceText.Text = _statement.OpeningBalance.ToString("C");
                ClosingBalanceText.Text = _statement.ClosingBalance.ToString("C");

                // Display line items
                StatementItemsGrid.ItemsSource = _statement.LineItems.OrderBy(li => li.SortOrder).ToList();
            }
        }

        private async void ExportPdfButton_Click(object sender, RoutedEventArgs e)
        {
            if (_statement == null)
            {
                MessageBox.Show("No statement loaded.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                // Get settings
                var settings = await _settingsService.GetSettingsAsync();

                // Generate PDF
                var pdfData = await _pdfService.GenerateStatementPdfAsync(_statement, settings);

                // Ask user where to save
                var saveDialog = new SaveFileDialog
                {
                    Filter = "PDF Files (*.pdf)|*.pdf",
                    FileName = $"Statement_{_statement.Client.CompanyName}_{_statement.StartDate:yyyyMMdd}-{_statement.EndDate:yyyyMMdd}.pdf",
                    DefaultExt = ".pdf"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    await _pdfService.SavePdfAsync(pdfData, saveDialog.FileName);

                    MessageBox.Show($"Statement PDF exported successfully to:\n{saveDialog.FileName}", "Success",
                        MessageBoxButton.OK, MessageBoxImage.Information);

                    // Ask if they want to open it
                    var result = MessageBox.Show("Would you like to open the PDF now?", "Open PDF",
                        MessageBoxButton.YesNo, MessageBoxImage.Question);

                    if (result == MessageBoxResult.Yes)
                    {
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = saveDialog.FileName,
                            UseShellExecute = true
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error exporting PDF: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void EmailStatementButton_Click(object sender, RoutedEventArgs e)
        {
            if (_statement == null)
            {
                MessageBox.Show("No statement loaded.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (string.IsNullOrWhiteSpace(_statement.Client.Email))
            {
                MessageBox.Show("This client does not have an email address. Please add one in the client details.", "No Email",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                // Get settings
                var settings = await _settingsService.GetSettingsAsync();

                // Generate PDF
                var pdfData = await _pdfService.GenerateStatementPdfAsync(_statement, settings);

                // Save to temp file
                var tempPath = Path.Combine(Path.GetTempPath(), $"Statement_{_statement.Client.CompanyName}_{DateTime.Now:yyyyMMddHHmmss}.pdf");
                await _pdfService.SavePdfAsync(pdfData, tempPath);

                // Prepare email
                var subject = $"Statement from {settings.CompanyName} - {_statement.StartDate:MMM yyyy}";
                var body = $"Dear {_statement.Client.CompanyName},\n\n" +
                          $"Please find attached your statement for the period {_statement.StartDate:d} to {_statement.EndDate:d}.\n\n" +
                          $"Opening Balance: {settings.CurrencySymbol}{_statement.OpeningBalance:N2}\n" +
                          $"Closing Balance: {settings.CurrencySymbol}{_statement.ClosingBalance:N2}\n\n" +
                          $"If you have any questions, please don't hesitate to contact us.\n\n" +
                          $"Best regards,\n{settings.CompanyName}";

                // Create email dialog
                var emailDialog = new StatementEmailDialog(
                    _emailService,
                    _pdfService,
                    _settingsService,
                    _statement,
                    pdfData,
                    subject,
                    body);

                if (emailDialog.ShowDialog() == true)
                {
                    MessageBox.Show("Statement emailed successfully!", "Success",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }

                // Clean up temp file
                try { File.Delete(tempPath); } catch { }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error sending email: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}