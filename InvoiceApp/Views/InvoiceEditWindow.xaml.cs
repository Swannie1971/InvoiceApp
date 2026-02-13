using InvoiceApp.Models;
using InvoiceApp.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;

namespace InvoiceApp.Views
{
    public partial class InvoiceEditWindow : Window
    {
        private readonly IInvoiceService _invoiceService;
        private readonly IClientService _clientService;
        private readonly IProductService _productService;
        private readonly ISettingsService _settingsService;
        private readonly IPdfService? _pdfService;
        private readonly IEmailService? _emailService;
        private readonly int? _invoiceId;
        private Invoice? _invoice;
        private string _currencySymbol = "R";
        private ObservableCollection<InvoiceLineItem> _lineItems;

        public InvoiceEditWindow(
            IInvoiceService invoiceService,
            IClientService clientService,
            IProductService productService,
            ISettingsService settingsService,
            IPdfService? pdfService,
            IEmailService? emailService = null,
            int? invoiceId = null)
        {
            InitializeComponent();

            _invoiceService = invoiceService;
            _clientService = clientService;
            _productService = productService;
            _settingsService = settingsService;
            _pdfService = pdfService;
            _emailService = emailService;
            _invoiceId = invoiceId;
            _lineItems = new ObservableCollection<InvoiceLineItem>();

            LineItemsDataGrid.ItemsSource = _lineItems;
            _lineItems.CollectionChanged += (s, e) => CalculateTotals();

            LoadDataAsync();
        }

        private async void LoadDataAsync()
        {
            // Load clients
            var clients = await _clientService.GetActiveClientsAsync();
            ClientComboBox.ItemsSource = clients;

            // Load settings for defaults
            var settings = await _settingsService.GetSettingsAsync(); // ← First, get settings
            _currencySymbol = settings.CurrencySymbol; // ← Then use settings
            PaymentTermsTextBox.Text = settings.DefaultPaymentTerms;

            if (_invoiceId.HasValue)
            {
                // Edit mode - load existing invoice
                _invoice = await _invoiceService.GetInvoiceByIdAsync(_invoiceId.Value);
                if (_invoice != null)
                {
                    InvoiceNumberTextBox.Text = _invoice.InvoiceNumber;
                    ClientComboBox.SelectedValue = _invoice.ClientId;
                    InvoiceDatePicker.SelectedDate = _invoice.InvoiceDate;
                    DueDatePicker.SelectedDate = _invoice.DueDate;
                    NotesTextBox.Text = _invoice.Notes;
                    PaymentTermsTextBox.Text = _invoice.PaymentTerms;

                    foreach (var item in _invoice.LineItems.OrderBy(li => li.SortOrder))
                    {
                        _lineItems.Add(item);
                    }

                    Title = "Edit Invoice";
                }
            }
            else
            {
                // New invoice mode
                InvoiceNumberTextBox.Text = await _invoiceService.GenerateInvoiceNumberAsync();
                InvoiceDatePicker.SelectedDate = DateTime.Today;
                DueDatePicker.SelectedDate = DateTime.Today.AddDays(30);

                if (clients.Any())
                {
                    ClientComboBox.SelectedIndex = 0;
                }

                Title = "New Invoice";
            }

            CalculateTotals();
        }

        private void AddItemButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new AddLineItemDialog(_productService, _settingsService);
            if (dialog.ShowDialog() == true && dialog.LineItem != null)
            {
                dialog.LineItem.SortOrder = _lineItems.Count;
                _lineItems.Add(dialog.LineItem);
            }
        }

        private void RemoveItemButton_Click(object sender, RoutedEventArgs e)
        {
            if (LineItemsDataGrid.SelectedItem is InvoiceLineItem selectedItem)
            {
                _lineItems.Remove(selectedItem);
            }
        }

        private void CalculateTotals()
        {
            decimal subtotal = 0;
            decimal taxAmount = 0;

            foreach (var item in _lineItems)
            {
                var lineSubtotal = item.Quantity * item.UnitPrice;
                var lineTax = lineSubtotal * (item.TaxRate / 100);
                item.LineTotal = lineSubtotal + lineTax;

                subtotal += lineSubtotal;
                taxAmount += lineTax;
            }

            SubtotalTextBlock.Text = $"{_currencySymbol}{subtotal:N2}";
            TaxTextBlock.Text = $"{_currencySymbol}{taxAmount:N2}";
            TotalTextBlock.Text = $"{_currencySymbol}{(subtotal + taxAmount):N2}";
        }
        private async void ExportPdfButton_Click(object sender, RoutedEventArgs e)
        {
            if (_pdfService == null)
            {
                MessageBox.Show("PDF service is not available.", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            // Validate that invoice has required data
            if (ClientComboBox.SelectedValue == null)
            {
                MessageBox.Show("Please select a client before exporting.", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!_lineItems.Any())
            {
                MessageBox.Show("Please add at least one line item before exporting.", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                // Get the client
                var client = await _clientService.GetClientByIdAsync((int)ClientComboBox.SelectedValue);
                if (client == null)
                {
                    MessageBox.Show("Could not find client information.", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Create a temporary invoice object for PDF generation
                var tempInvoice = new Invoice
                {
                    Id = _invoiceId ?? 0,
                    InvoiceNumber = InvoiceNumberTextBox.Text,
                    ClientId = (int)ClientComboBox.SelectedValue,
                    Client = client,
                    InvoiceDate = InvoiceDatePicker.SelectedDate ?? DateTime.Today,
                    DueDate = DueDatePicker.SelectedDate ?? DateTime.Today.AddDays(30),
                    Notes = NotesTextBox.Text,
                    PaymentTerms = PaymentTermsTextBox.Text,
                    Status = _invoice?.Status ?? InvoiceStatus.Draft,
                    LineItems = new List<InvoiceLineItem>()
                };

                // Copy line items
                foreach (var item in _lineItems)
                {
                    tempInvoice.LineItems.Add(new InvoiceLineItem
                    {
                        Description = item.Description,
                        Quantity = item.Quantity,
                        UnitPrice = item.UnitPrice,
                        TaxRate = item.TaxRate,
                        SortOrder = item.SortOrder
                    });
                }

                // Calculate totals
                decimal subtotal = 0;
                decimal taxAmount = 0;
                int sortOrder = 0;

                foreach (var item in tempInvoice.LineItems)
                {
                    item.SortOrder = sortOrder++;
                    var lineSubtotal = item.Quantity * item.UnitPrice;
                    var lineTax = lineSubtotal * (item.TaxRate / 100);
                    item.LineTotal = lineSubtotal + lineTax;
                    subtotal += lineSubtotal;
                    taxAmount += lineTax;
                }

                tempInvoice.Subtotal = subtotal;
                tempInvoice.TaxAmount = taxAmount;
                tempInvoice.Total = subtotal + taxAmount;

                // Get settings for PDF
                var settings = await _settingsService.GetSettingsAsync();

                // Generate PDF
                var pdfData = await _pdfService.GenerateInvoicePdfAsync(tempInvoice, settings);

                // Ask user where to save
                var saveDialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "PDF Files (*.pdf)|*.pdf",
                    FileName = $"Invoice_{tempInvoice.InvoiceNumber}.pdf",
                    DefaultExt = ".pdf"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    await _pdfService.SavePdfAsync(pdfData, saveDialog.FileName);

                    MessageBox.Show($"PDF exported successfully to:\n{saveDialog.FileName}", "Success",
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
                MessageBox.Show($"Error exporting PDF: {ex.Message}\n\nDetails: {ex.InnerException?.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (ClientComboBox.SelectedValue == null)
            {
                MessageBox.Show("Please select a client.", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!InvoiceDatePicker.SelectedDate.HasValue || !DueDatePicker.SelectedDate.HasValue)
            {
                MessageBox.Show("Please select invoice and due dates.", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!_lineItems.Any())
            {
                MessageBox.Show("Please add at least one line item.", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                if (_invoice == null)
                {
                    _invoice = new Invoice
                    {
                        InvoiceNumber = InvoiceNumberTextBox.Text
                    };
                }

                _invoice.ClientId = (int)ClientComboBox.SelectedValue;
                _invoice.InvoiceDate = InvoiceDatePicker.SelectedDate.Value;
                _invoice.DueDate = DueDatePicker.SelectedDate.Value;
                _invoice.Notes = NotesTextBox.Text;
                _invoice.PaymentTerms = PaymentTermsTextBox.Text;
                _invoice.LineItems = _lineItems.ToList();

                if (_invoiceId.HasValue)
                {
                    await _invoiceService.UpdateInvoiceAsync(_invoice);
                    MessageBox.Show("Invoice updated successfully!", "Success",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    await _invoiceService.CreateInvoiceAsync(_invoice);
                    MessageBox.Show("Invoice created successfully!", "Success",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving invoice: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
        private async void SendEmailButton_Click(object sender, RoutedEventArgs e)
        {
            if (_pdfService == null)
            {
                MessageBox.Show("PDF service is not available.", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (_emailService == null)
            {
                MessageBox.Show("Email service is not available.", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Validate that invoice has required data
            if (ClientComboBox.SelectedValue == null)
            {
                MessageBox.Show("Please select a client before sending email.", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!_lineItems.Any())
            {
                MessageBox.Show("Please add at least one line item before sending email.", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                // Get the client
                var client = await _clientService.GetClientByIdAsync((int)ClientComboBox.SelectedValue);
                if (client == null)
                {
                    MessageBox.Show("Could not find client information.", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Check if client has email
                if (string.IsNullOrWhiteSpace(client.Email))
                {
                    MessageBox.Show("This client does not have an email address. Please add one in the client details.", "No Email",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Create a temporary invoice object
                var tempInvoice = new Invoice
                {
                    Id = _invoiceId ?? 0,
                    InvoiceNumber = InvoiceNumberTextBox.Text,
                    ClientId = (int)ClientComboBox.SelectedValue,
                    Client = client,
                    InvoiceDate = InvoiceDatePicker.SelectedDate ?? DateTime.Today,
                    DueDate = DueDatePicker.SelectedDate ?? DateTime.Today.AddDays(30),
                    Notes = NotesTextBox.Text,
                    PaymentTerms = PaymentTermsTextBox.Text,
                    Status = _invoice?.Status ?? InvoiceStatus.Draft,
                    LineItems = new List<InvoiceLineItem>()
                };

                // Copy line items
                foreach (var item in _lineItems)
                {
                    tempInvoice.LineItems.Add(new InvoiceLineItem
                    {
                        Description = item.Description,
                        Quantity = item.Quantity,
                        UnitPrice = item.UnitPrice,
                        TaxRate = item.TaxRate,
                        SortOrder = item.SortOrder
                    });
                }

                // Calculate totals
                decimal subtotal = 0;
                decimal taxAmount = 0;
                int sortOrder = 0;

                foreach (var item in tempInvoice.LineItems)
                {
                    item.SortOrder = sortOrder++;
                    var lineSubtotal = item.Quantity * item.UnitPrice;
                    var lineTax = lineSubtotal * (item.TaxRate / 100);
                    item.LineTotal = lineSubtotal + lineTax;
                    subtotal += lineSubtotal;
                    taxAmount += lineTax;
                }

                tempInvoice.Subtotal = subtotal;
                tempInvoice.TaxAmount = taxAmount;
                tempInvoice.Total = subtotal + taxAmount;

                // Get settings for PDF
                var settings = await _settingsService.GetSettingsAsync();

                // Generate PDF
                var pdfData = await _pdfService.GenerateInvoicePdfAsync(tempInvoice, settings);

                // Open email dialog
                var emailDialog = new EmailSendDialog(_emailService, _pdfService, _settingsService, tempInvoice, pdfData);
                emailDialog.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error preparing email: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

}