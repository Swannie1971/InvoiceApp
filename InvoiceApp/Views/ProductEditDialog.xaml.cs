using System;
using System.Windows;
using InvoiceApp.Models;
using InvoiceApp.Services;

namespace InvoiceApp.Views
{
    public partial class ProductEditDialog : Window
    {
        private readonly IProductService _productService;
        private readonly int? _productId;
        private Product? _product;

        public ProductEditDialog(IProductService productService, int? productId = null)
        {
            InitializeComponent();
            _productService = productService;
            _productId = productId;

            if (_productId.HasValue)
            {
                LoadProductAsync();
            }
        }

        private async void LoadProductAsync()
        {
            _product = await _productService.GetProductByIdAsync(_productId!.Value);
            if (_product != null)
            {
                NameTextBox.Text = _product.Name;
                DescriptionTextBox.Text = _product.Description;
                SkuTextBox.Text = _product.Sku;
                UnitPriceTextBox.Text = _product.UnitPrice.ToString("0.00");
                TaxRateTextBox.Text = _product.TaxRate?.ToString("0.00") ?? "0";
                IsActiveCheckBox.IsChecked = _product.IsActive;

                Title = "Edit Product/Service";
            }
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(NameTextBox.Text))
            {
                MessageBox.Show("Product name is required.", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                NameTextBox.Focus();
                return;
            }

            if (!decimal.TryParse(UnitPriceTextBox.Text, out decimal unitPrice) || unitPrice < 0)
            {
                MessageBox.Show("Please enter a valid unit price.", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                UnitPriceTextBox.Focus();
                return;
            }

            decimal? taxRate = null;
            if (!string.IsNullOrWhiteSpace(TaxRateTextBox.Text))
            {
                if (decimal.TryParse(TaxRateTextBox.Text, out decimal parsedTaxRate))
                {
                    taxRate = parsedTaxRate;
                }
            }

            // Check SKU uniqueness
            if (!string.IsNullOrWhiteSpace(SkuTextBox.Text))
            {
                bool skuExists = await _productService.SkuExistsAsync(SkuTextBox.Text.Trim(), _productId);
                if (skuExists)
                {
                    MessageBox.Show("This SKU already exists. Please use a unique SKU.", "Validation Error",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    SkuTextBox.Focus();
                    return;
                }
            }

            try
            {
                if (_product == null)
                {
                    _product = new Product();
                }

                _product.Name = NameTextBox.Text.Trim();
                _product.Description = DescriptionTextBox.Text.Trim();
                _product.Sku = SkuTextBox.Text.Trim();
                _product.UnitPrice = unitPrice;
                _product.TaxRate = taxRate;
                _product.IsActive = IsActiveCheckBox.IsChecked ?? true;

                if (_productId.HasValue)
                {
                    await _productService.UpdateProductAsync(_product);
                    MessageBox.Show("Product updated successfully!", "Success",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    await _productService.CreateProductAsync(_product);
                    MessageBox.Show("Product created successfully!", "Success",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving product: {ex.Message}", "Error",
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