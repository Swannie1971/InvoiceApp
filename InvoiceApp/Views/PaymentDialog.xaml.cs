using System;
using System.Windows;
using InvoiceApp.Models;
using InvoiceApp.Services;

namespace InvoiceApp.Views
{
    public partial class PaymentDialog : Window
    {
        private readonly IInvoiceService _invoiceService;
        private readonly ISettingsService _settingsService;
        private readonly Invoice _invoice;
        private string _currencySymbol = "$";

        public PaymentDialog(IInvoiceService invoiceService, ISettingsService settingsService, Invoice invoice)
        {
            InitializeComponent();

            _invoiceService = invoiceService;
            _settingsService = settingsService;
            _invoice = invoice;

            LoadDataAsync();
        }

        private async void LoadDataAsync()
        {
            // Get currency symbol from settings
            var settings = await _settingsService.GetSettingsAsync();
            _currencySymbol = settings.CurrencySymbol;

            // Set display info
            InvoiceInfoText.Text = $"Invoice: {_invoice.InvoiceNumber} - {_invoice.Client.CompanyName}";
            AmountDueText.Text = $"{_currencySymbol}{_invoice.Total:N2}";

            // Set defaults
            PaymentAmountTextBox.Text = _invoice.Total.ToString("F2");
            PaymentDatePicker.SelectedDate = DateTime.Today;
        }

        private async void RecordPaymentButton_Click(object sender, RoutedEventArgs e)
        {
            // Validate
            if (!decimal.TryParse(PaymentAmountTextBox.Text, out decimal paymentAmount) || paymentAmount <= 0)
            {
                MessageBox.Show("Please enter a valid payment amount.", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                PaymentAmountTextBox.Focus();
                return;
            }

            if (!PaymentDatePicker.SelectedDate.HasValue)
            {
                MessageBox.Show("Please select a payment date.", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (paymentAmount > _invoice.Total)
            {
                var result = MessageBox.Show(
                    $"Payment amount ({_currencySymbol}{paymentAmount:N2}) is greater than invoice total ({_currencySymbol}{_invoice.Total:N2}).\n\nDo you want to continue?",
                    "Confirm Overpayment",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.No)
                {
                    return;
                }
            }

            try
            {
                // Mark invoice as paid
                _invoice.Status = InvoiceStatus.Paid;
                _invoice.PaidDate = PaymentDatePicker.SelectedDate.Value;

                // Add payment note if provided
                if (!string.IsNullOrWhiteSpace(NotesTextBox.Text))
                {
                    _invoice.Notes = (_invoice.Notes ?? "") + $"\n\nPayment received on {PaymentDatePicker.SelectedDate.Value:d}: {NotesTextBox.Text}";
                }

                await _invoiceService.UpdateInvoiceAsync(_invoice);

                MessageBox.Show($"Payment of {_currencySymbol}{paymentAmount:N2} recorded successfully!\n\nInvoice {_invoice.InvoiceNumber} is now marked as PAID.",
                    "Payment Recorded", MessageBoxButton.OK, MessageBoxImage.Information);

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error recording payment: {ex.Message}", "Error",
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