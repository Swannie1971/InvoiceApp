using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using InvoiceApp.Services;

namespace InvoiceApp.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        private readonly IProductService _productService;
        private readonly IClientService _clientService;
        private readonly IInvoiceService _invoiceService;
        private readonly IStatementService _statementService;
        private readonly ISettingsService _settingsService;
        private readonly IEmailService _emailService;
        private readonly IAuthenticationService _authService;

        private ViewModelBase? _currentViewModel;
        private string _title = "Invoice App";

        private readonly IPdfService _pdfService;
        public MainWindowViewModel(
            IProductService productService,
            IClientService clientService,
            IInvoiceService invoiceService,
            IStatementService statementService,
            ISettingsService settingsService,
            IPdfService pdfService,
            IEmailService emailService,
            IAuthenticationService authService)
        {
            _productService = productService;
            _clientService = clientService;
            _invoiceService = invoiceService;
            _statementService = statementService;
            _settingsService = settingsService;
            _pdfService = pdfService;
            _emailService = emailService;
            _authService = authService;

            // Initialize commands
            ShowDashboardCommand = new RelayCommand(ShowDashboard);
            ShowInvoicesCommand = new RelayCommand(ShowInvoices);
            ShowClientsCommand = new RelayCommand(ShowClients);
            ShowProductsCommand = new RelayCommand(ShowProducts);
            ShowStatementsCommand = new RelayCommand(ShowStatements);
            ShowSettingsCommand = new RelayCommand(ShowSettings);
            LogoutCommand = new RelayCommand(Logout);
            ShowReportsCommand = new RelayCommand(ShowReports);

            // Show dashboard by default
            ShowDashboard();
        }

        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        public ViewModelBase? CurrentViewModel
        {
            get => _currentViewModel;
            set => SetProperty(ref _currentViewModel, value);
        }

        public ICommand ShowDashboardCommand { get; }
        public ICommand ShowInvoicesCommand { get; }
        public ICommand ShowClientsCommand { get; }
        public ICommand ShowProductsCommand { get; }
        public ICommand ShowStatementsCommand { get; }
        public ICommand ShowSettingsCommand { get; }
        public ICommand LogoutCommand { get; }
        public ICommand ShowReportsCommand { get; }
        private void ShowDashboard()
        {
            CurrentViewModel = new DashboardViewModel(_invoiceService, _clientService);
        }

        private void ShowInvoices()
        {
            CurrentViewModel = new InvoiceListViewModel(_invoiceService, _clientService, _productService, _settingsService, _pdfService, _emailService, _authService);
        }

        private void ShowClients()
        {
            CurrentViewModel = new ClientListViewModel(_clientService);
        }

        private void ShowProducts()
        {
            CurrentViewModel = new ProductListViewModel(_productService);
        }

        private void ShowStatements()
        {
            CurrentViewModel = new StatementListViewModel(_statementService, _clientService, _pdfService, _emailService, _settingsService);
        }

        private void ShowSettings()
        {
            CurrentViewModel = new SettingsViewModel(_settingsService);
        }
        private void Logout()
        {
            var result = MessageBox.Show(
                "Are you sure you want to logout?",
                "Confirm Logout",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                _authService.Logout();

                // Close all windows
                foreach (Window window in Application.Current.Windows)
                {
                    window.Close();
                }

                // Restart the app by creating new login window
                var app = (App)Application.Current;
                app.ShowLoginWindow();
            }
        }
        private void ShowReports()
        {
            CurrentViewModel = new ReportsViewModel(_invoiceService, _clientService, _settingsService);
        }
    }
}


