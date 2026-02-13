using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using InvoiceApp.Models;
using InvoiceApp.Services;
using Microsoft.Win32;

namespace InvoiceApp.Views
{
    public partial class EmailSendDialog : Window
    {
        private readonly IEmailService _emailService;
        private readonly IPdfService _pdfService;
        private readonly ISettingsService _settingsService;
        private readonly Invoice _invoice;
        private readonly byte[] _invoicePdf;
        private readonly List<string> _attachmentPaths = new();

        public EmailSendDialog(
            IEmailService emailService,
            IPdfService pdfService,
            ISettingsService settingsService,
            Invoice invoice,
            byte[] invoicePdf)
        {
            InitializeComponent();

            _emailService = emailService;
            _pdfService = pdfService;
            _settingsService = settingsService;
            _invoice = invoice;
            _invoicePdf = invoicePdf;

            LoadDefaultsAsync();
        }

        private async void LoadDefaultsAsync()
        {
            var settings = await _settingsService.GetSettingsAsync();

            // Set default recipient
            ToEmailTextBox.Text = _invoice.Client.Email ?? string.Empty;

            // Set default subject
            var subject = settings.DefaultEmailSubject
                .Replace("{InvoiceNumber}", _invoice.InvoiceNumber)
                .Replace("{CompanyName}", settings.CompanyName);
            SubjectTextBox.Text = subject;

            // Set default body
            var body = settings.DefaultEmailBody
                .Replace("{ClientName}", _invoice.Client.CompanyName)
                .Replace("{InvoiceNumber}", _invoice.InvoiceNumber)
                .Replace("{CompanyName}", settings.CompanyName);
            BodyTextBox.Text = body;

            // Invoice PDF is shown in the green text above - no need to add to ListBox
        }

        private void AddAttachmentButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Common Files|*.pdf;*.jpg;*.jpeg;*.png;*.docx;*.xlsx;*.txt|" +
                         "PDF Files (*.pdf)|*.pdf|" +
                         "Images (*.jpg;*.jpeg;*.png)|*.jpg;*.jpeg;*.png|" +
                         "Word Documents (*.docx;*.doc)|*.docx;*.doc|" +
                         "Excel Files (*.xlsx;*.xls)|*.xlsx;*.xls|" +
                         "All Files (*.*)|*.*",
                Title = "Select Receipt or Supporting Document",
                Multiselect = true
            };

            if (dialog.ShowDialog() == true)
            {
                foreach (var fileName in dialog.FileNames)
                {
                    if (!_attachmentPaths.Contains(fileName))
                    {
                        _attachmentPaths.Add(fileName);
                        var fileInfo = new FileInfo(fileName);
                        var sizeKB = fileInfo.Length / 1024;
                        AttachmentsListBox.Items.Add($"{Path.GetFileName(fileName)} ({sizeKB:N0} KB)");
                    }
                }

                // Show helpful message on first attachment
                if (_attachmentPaths.Count == 1)
                {
                    MessageBox.Show("Receipt added! You can add multiple files if needed.\n\nThe invoice PDF is already attached automatically.",
                        "Attachment Added", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }

        private void RemoveAttachmentButton_Click(object sender, RoutedEventArgs e)
        {
            if (AttachmentsListBox.SelectedIndex >= 0)
            {
                int index = AttachmentsListBox.SelectedIndex;

                if (index < _attachmentPaths.Count)
                {
                    var fileName = _attachmentPaths[index];
                    var result = MessageBox.Show($"Remove this attachment?\n\n{Path.GetFileName(fileName)}",
                        "Confirm Remove", MessageBoxButton.YesNo, MessageBoxImage.Question);

                    if (result == MessageBoxResult.Yes)
                    {
                        _attachmentPaths.RemoveAt(index);
                        AttachmentsListBox.Items.RemoveAt(index);
                    }
                }
            }
            else
            {
                MessageBox.Show("Please select a file to remove from the list.", "No Selection",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
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
                // Save invoice PDF temporarily
                var tempInvoicePath = Path.Combine(Path.GetTempPath(), $"Invoice_{_invoice.InvoiceNumber}.pdf");
                await _pdfService.SavePdfAsync(_invoicePdf, tempInvoicePath);

                // Create list of all attachments
                var allAttachments = new List<string> { tempInvoicePath };
                allAttachments.AddRange(_attachmentPaths);

                // Send email with attachments
                var success = await _emailService.SendInvoiceEmailWithAttachmentsAsync(
                    _invoice,
                    ToEmailTextBox.Text,
                    SubjectTextBox.Text,
                    BodyTextBox.Text,
                    allAttachments.ToArray());

                // Clean up temp file
                try { File.Delete(tempInvoicePath); } catch { }

                if (success)
                {
                    MessageBox.Show("Email sent successfully!", "Success",
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

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}