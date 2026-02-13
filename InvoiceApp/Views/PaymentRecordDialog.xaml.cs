using InvoiceApp.Models;
using InvoiceApp.Services;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace InvoiceApp.Views
{
    public partial class PaymentRecordDialog : Window
    {
        private readonly IInvoiceService _invoiceService;
        private readonly ISettingsService _settingsService;
        private readonly IAuthenticationService _authService;
        private Invoice _invoice; // Removed readonly so we can reassign it
        private string _currencySymbol = "$";

        public PaymentRecordDialog(
            IInvoiceService invoiceService,
            ISettingsService settingsService,
            IAuthenticationService authService,
            Invoice invoice)
        {
            InitializeComponent();

            _invoiceService = invoiceService;
            _settingsService = settingsService;
            _authService = authService;
            _invoice = invoice;

            LoadDataAsync();
        }

        private async Task LoadDataAsync()
        {
            // Reload invoice to get fresh data
            var freshInvoice = await _invoiceService.GetInvoiceByIdAsync(_invoice.Id);
            if (freshInvoice != null)
            {
                _invoice = freshInvoice;
            }

            // Get currency symbol from settings
            var settings = await _settingsService.GetSettingsAsync();
            _currencySymbol = settings.CurrencySymbol;

            // Set display info
            InvoiceInfoText.Text = $"Invoice: {_invoice.InvoiceNumber} - {_invoice.Client.CompanyName}";

            // Display amounts
            TotalAmountText.Text = $"{_currencySymbol}{_invoice.Total:N2}";
            PaidAmountText.Text = $"{_currencySymbol}{_invoice.AmountPaid:N2}";
            DueAmountText.Text = $"{_currencySymbol}{_invoice.AmountRemaining:N2}";

            // Set default payment amount to remaining balance
            PaymentAmountTextBox.Text = _invoice.AmountRemaining.ToString("F2");
            PaymentDatePicker.SelectedDate = DateTime.Today;

            // Load payment history
            await LoadPaymentHistoryAsync();
        }

        private async Task LoadPaymentHistoryAsync()
        {
            // Reload invoice with payments
            var invoiceWithPayments = await _invoiceService.GetInvoiceByIdAsync(_invoice.Id);
            if (invoiceWithPayments != null)
            {
                PaymentHistoryGrid.ItemsSource = invoiceWithPayments.Payments.OrderByDescending(p => p.PaymentDate);
            }
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

            if (paymentAmount > _invoice.AmountRemaining)
            {
                var result = MessageBox.Show(
                    $"Payment amount ({_currencySymbol}{paymentAmount:N2}) is greater than the remaining balance ({_currencySymbol}{_invoice.AmountRemaining:N2}).\n\nDo you want to record an overpayment?",
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
                // Create payment record
                var payment = new Payment
                {
                    InvoiceId = _invoice.Id,
                    Amount = paymentAmount,
                    PaymentDate = PaymentDatePicker.SelectedDate.Value,
                    PaymentMethod = ((ComboBoxItem)PaymentMethodComboBox.SelectedItem).Content.ToString() ?? "Cash",
                    ReferenceNumber = string.IsNullOrWhiteSpace(ReferenceNumberTextBox.Text) ? null : ReferenceNumberTextBox.Text,
                    Notes = string.IsNullOrWhiteSpace(NotesTextBox.Text) ? null : NotesTextBox.Text,
                    RecordedByUserId = _authService.CurrentUser?.Id,
                    CreatedAt = DateTime.Now
                };

                // Add payment to invoice
                _invoice.Payments.Add(payment);

                // Save payment first
                await _invoiceService.UpdateInvoiceAsync(_invoice);

                // Reload invoice to get fresh payment calculations
                var reloadedInvoice = await _invoiceService.GetInvoiceByIdAsync(_invoice.Id);
                if (reloadedInvoice != null)
                {
                    _invoice = reloadedInvoice; // Update our reference

                    // Update invoice status based on ACTUAL total paid
                    if (reloadedInvoice.AmountRemaining <= 0.01m) // Use small tolerance for rounding
                    {
                        reloadedInvoice.Status = InvoiceStatus.Paid;
                        reloadedInvoice.PaidDate = PaymentDatePicker.SelectedDate.Value;
                    }
                    else if (reloadedInvoice.AmountPaid > 0)
                    {
                        reloadedInvoice.Status = InvoiceStatus.PartiallyPaid;
                    }
                    // else: No payments yet - keep existing status (Draft/Sent/Overdue)

                    // Save status update
                    await _invoiceService.UpdateInvoiceAsync(reloadedInvoice);

                    MessageBox.Show(
                        $"Payment of {_currencySymbol}{paymentAmount:N2} recorded successfully!\n\n" +
                        $"Total Paid: {_currencySymbol}{reloadedInvoice.AmountPaid:N2}\n" +
                        $"Remaining: {_currencySymbol}{reloadedInvoice.AmountRemaining:N2}\n\n" +
                        $"Status: {reloadedInvoice.Status}",
                        "Payment Recorded",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);

                    // Refresh display
                    await LoadDataAsync();

                    // Clear form for next payment
                    PaymentAmountTextBox.Text = _invoice.AmountRemaining.ToString("F2");
                    ReferenceNumberTextBox.Clear();
                    NotesTextBox.Clear();
                    PaymentDatePicker.SelectedDate = DateTime.Today;

                    // If fully paid, offer to close dialog
                    if (reloadedInvoice.Status == InvoiceStatus.Paid)
                    {
                        var closeResult = MessageBox.Show(
                            "Invoice is now fully paid. Close this dialog?",
                            "Invoice Paid",
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Question);

                        if (closeResult == MessageBoxResult.Yes)
                        {
                            DialogResult = true;
                            Close();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error recording payment: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}