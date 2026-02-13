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
    public class InvoiceListViewModel : ViewModelBase
    {
        private readonly IInvoiceService _invoiceService;
        private readonly IClientService _clientService;
        private readonly IProductService _productService;
        private readonly ISettingsService _settingsService;
        private readonly IPdfService _pdfService;
        private readonly IEmailService _emailService;
        private readonly IAuthenticationService _authService;
        private ObservableCollection<Invoice> _invoices = new();
        private Invoice? _selectedInvoice;
        private string _searchText = string.Empty;

        public InvoiceListViewModel(
            IInvoiceService invoiceService,
            IClientService clientService,
            IProductService productService,
            ISettingsService settingsService,
            IPdfService pdfService,
            IEmailService emailService,
            IAuthenticationService authService)
        {
            _invoiceService = invoiceService;
            _clientService = clientService;
            _productService = productService;
            _settingsService = settingsService;
            _pdfService = pdfService;
            _emailService = emailService;
            _authService = authService;

            CreateInvoiceCommand = new AsyncRelayCommand(CreateInvoiceAsync);
            EditInvoiceCommand = new RelayCommand(EditInvoice, () => SelectedInvoice != null);
            DeleteInvoiceCommand = new AsyncRelayCommand(DeleteInvoiceAsync, () => SelectedInvoice != null);
            DuplicateInvoiceCommand = new AsyncRelayCommand(DuplicateInvoiceAsync, () => SelectedInvoice != null);
            RefreshCommand = new AsyncRelayCommand(LoadInvoicesAsync);
            RecordPaymentCommand = new RelayCommand(RecordPayment, () => SelectedInvoice != null && SelectedInvoice.Status != InvoiceStatus.Paid);

            LoadInvoicesAsync();
        }

        public ObservableCollection<Invoice> Invoices
        {
            get => _invoices;
            set => SetProperty(ref _invoices, value);
        }

        public Invoice? SelectedInvoice
        {
            get => _selectedInvoice;
            set
            {
                if (SetProperty(ref _selectedInvoice, value))
                {
                    ((RelayCommand)EditInvoiceCommand).RaiseCanExecuteChanged();
                    ((AsyncRelayCommand)DeleteInvoiceCommand).RaiseCanExecuteChanged();
                    ((AsyncRelayCommand)DuplicateInvoiceCommand).RaiseCanExecuteChanged();
                    ((RelayCommand)RecordPaymentCommand).RaiseCanExecuteChanged();
                }
            }
        }

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                {
                    FilterInvoices();
                }
            }
        }

        public ICommand CreateInvoiceCommand { get; }
        public ICommand EditInvoiceCommand { get; }
        public ICommand DeleteInvoiceCommand { get; }
        public ICommand DuplicateInvoiceCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand RecordPaymentCommand { get; }

        private async Task LoadInvoicesAsync()
        {
            var invoices = await _invoiceService.GetAllInvoicesAsync();
            Invoices = new ObservableCollection<Invoice>(invoices);
        }

        private void FilterInvoices()
        {
            // Implement filtering logic based on SearchText
            LoadInvoicesAsync();
        }

        private async Task CreateInvoiceAsync()
        {
            var window = new Views.InvoiceEditWindow(_invoiceService, _clientService, _productService, _settingsService, _pdfService, _emailService);
            if (window.ShowDialog() == true)
            {
                await LoadInvoicesAsync();
            }
        }

        private void EditInvoice()
        {
            if (SelectedInvoice != null)
            {
                var window = new Views.InvoiceEditWindow(_invoiceService, _clientService, _productService, _settingsService, _pdfService, _emailService, SelectedInvoice.Id);
                if (window.ShowDialog() == true)
                {
                    LoadInvoicesAsync();
                }
            }
        }

        private async Task DeleteInvoiceAsync()
        {
            if (SelectedInvoice != null)
            {
                var result = MessageBox.Show(
                    $"Are you sure you want to delete invoice {SelectedInvoice.InvoiceNumber}?",
                    "Confirm Delete",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    await _invoiceService.DeleteInvoiceAsync(SelectedInvoice.Id);
                    await LoadInvoicesAsync();
                }
            }
        }

        private async Task DuplicateInvoiceAsync()
        {
            if (SelectedInvoice != null)
            {
                await _invoiceService.DuplicateInvoiceAsync(SelectedInvoice.Id);
                await LoadInvoicesAsync();
            }
        }
        private void RecordPayment()
        {
            if (SelectedInvoice != null)
            {
                var dialog = new Views.PaymentRecordDialog(_invoiceService, _settingsService, _authService, SelectedInvoice);
                if (dialog.ShowDialog() == true)
                {
                    LoadInvoicesAsync();
                }
            }
        }
    }
}


