using System;
using System.Linq;
using System.Windows;
using InvoiceApp.Models;
using InvoiceApp.Services;

namespace InvoiceApp.Views
{
    public partial class AddLineItemDialog : Window
    {
        private readonly IProductService _productService;
        private readonly ISettingsService _settingsService;

        public InvoiceLineItem? LineItem { get; private set; }

        public AddLineItemDialog(IProductService productService, ISettingsService settingsService)
        {
            InitializeComponent();

            _productService = productService;
            _settingsService = settingsService;

            LoadProductsAsync();
        }

        private async void LoadProductsAsync()
        {
            var products = await _productService.GetActiveProductsAsync();
            ProductComboBox.ItemsSource = products;

            // Set default tax rate
            var settings = await _settingsService.GetSettingsAsync();
            TaxRateTextBox.Text = settings.DefaultTaxRate.ToString("0.00");
        }

        private void ProductComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (ProductComboBox.SelectedItem is Product product)
            {
                DescriptionTextBox.Text = product.Name;
                UnitPriceTextBox.Text = product.UnitPrice.ToString("0.00");
                TaxRateTextBox.Text = (product.TaxRate ?? 0).ToString("0.00");
            }
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(DescriptionTextBox.Text))
            {
                MessageBox.Show("Description is required.", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!decimal.TryParse(QuantityTextBox.Text, out decimal quantity) || quantity <= 0)
            {
                MessageBox.Show("Please enter a valid quantity.", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!decimal.TryParse(UnitPriceTextBox.Text, out decimal unitPrice) || unitPrice < 0)
            {
                MessageBox.Show("Please enter a valid unit price.", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!decimal.TryParse(TaxRateTextBox.Text, out decimal taxRate) || taxRate < 0)
            {
                MessageBox.Show("Please enter a valid tax rate.", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            LineItem = new InvoiceLineItem
            {
                ProductId = (ProductComboBox.SelectedItem as Product)?.Id,
                Description = DescriptionTextBox.Text.Trim(),
                Quantity = quantity,
                UnitPrice = unitPrice,
                TaxRate = taxRate
            };

            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}