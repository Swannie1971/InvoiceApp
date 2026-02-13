using System;
using System.Windows;
using InvoiceApp.ViewModels;
using InvoiceApp.Services;

namespace InvoiceApp.Views
{
    public partial class MainWindow : Window
    {
        private readonly IAuthenticationService _authService;

        public MainWindow(MainWindowViewModel viewModel, IAuthenticationService authService)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("=== MainWindow Constructor START ===");

                InitializeComponent();
                System.Diagnostics.Debug.WriteLine("InitializeComponent OK");

                _authService = authService;
                DataContext = viewModel;
                System.Diagnostics.Debug.WriteLine("DataContext set");

                // Set this AFTER InitializeComponent
                Loaded += MainWindow_Loaded;
                System.Diagnostics.Debug.WriteLine("Loaded event subscribed");

                System.Diagnostics.Debug.WriteLine("=== MainWindow Constructor END ===");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"MainWindow Constructor EXCEPTION: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack: {ex.StackTrace}");
                MessageBox.Show($"MainWindow constructor error:\n\n{ex.Message}\n\n{ex.StackTrace}", "Error");
                throw;
            }
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("=== MainWindow_Loaded START ===");

                // Display current user info
                var currentUser = _authService.CurrentUser;
                System.Diagnostics.Debug.WriteLine($"CurrentUser: {currentUser?.FullName ?? "NULL"}");

                if (currentUser != null)
                {
                    CurrentUserText.Text = currentUser.FullName;
                    UserRoleText.Text = currentUser.Role.ToString();
                    System.Diagnostics.Debug.WriteLine("User info displayed");
                }

                System.Diagnostics.Debug.WriteLine("=== MainWindow_Loaded END ===");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"MainWindow_Loaded EXCEPTION: {ex.Message}");
                MessageBox.Show($"MainWindow_Loaded error:\n\n{ex.Message}", "Error");
            }
        }
    }
}