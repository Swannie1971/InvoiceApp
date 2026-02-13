using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using System;
using InvoiceApp.Models;
using InvoiceApp.Services;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;

namespace InvoiceApp.ViewModels
{
    public class ProductListViewModel : ViewModelBase
    {
        private readonly IProductService _productService;
        private ObservableCollection<Product> _products = new();
        private Product? _selectedProduct;

        public ProductListViewModel(IProductService productService)
        {
            _productService = productService;

            CreateProductCommand = new AsyncRelayCommand(CreateProductAsync);
            EditProductCommand = new RelayCommand(EditProduct, () => SelectedProduct != null);
            DeleteProductCommand = new AsyncRelayCommand(DeleteProductAsync, () => SelectedProduct != null);
            RefreshCommand = new AsyncRelayCommand(LoadProductsAsync);

            LoadProductsAsync();
        }

        public ObservableCollection<Product> Products
        {
            get => _products;
            set => SetProperty(ref _products, value);
        }

        public Product? SelectedProduct
        {
            get => _selectedProduct;
            set
            {
                if (SetProperty(ref _selectedProduct, value))
                {
                    ((RelayCommand)EditProductCommand).RaiseCanExecuteChanged();
                    ((AsyncRelayCommand)DeleteProductCommand).RaiseCanExecuteChanged();
                }
            }
        }

        public ICommand CreateProductCommand { get; }
        public ICommand EditProductCommand { get; }
        public ICommand DeleteProductCommand { get; }
        public ICommand RefreshCommand { get; }

        private async Task LoadProductsAsync()
        {
            var products = await _productService.GetAllProductsAsync();
            Products = new ObservableCollection<Product>(products);
        }

        private async Task CreateProductAsync()
        {
            var dialog = new Views.ProductEditDialog(_productService);
            if (dialog.ShowDialog() == true)
            {
                await LoadProductsAsync();
            }
        }

        private void EditProduct()
        {
            if (SelectedProduct != null)
            {
                var dialog = new Views.ProductEditDialog(_productService, SelectedProduct.Id);
                if (dialog.ShowDialog() == true)
                {
                    LoadProductsAsync();
                }
            }
        }

        private async Task DeleteProductAsync()
        {
            if (SelectedProduct != null)
            {
                var result = MessageBox.Show(
                    $"Are you sure you want to delete {SelectedProduct.Name}?",
                    "Confirm Delete",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    await _productService.DeleteProductAsync(SelectedProduct.Id);
                    await LoadProductsAsync();
                }
            }
        }
    }
}


