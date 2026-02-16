using System;
using System.IO;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using InvoiceApp.Data;
using InvoiceApp.Services;
using InvoiceApp.Views;
using InvoiceApp.ViewModels;

namespace InvoiceApp
{
    public partial class App : Application
    {
        private IServiceProvider? _serviceProvider;
        public IServiceProvider Services => _serviceProvider!;

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            try
            {
                // Setup dependency injection
                var services = new ServiceCollection();
                ConfigureServices(services);
                _serviceProvider = services.BuildServiceProvider();

                if (_serviceProvider == null)
                {
                    MessageBox.Show("Failed to initialize dependency injection.", "Startup Error");
                    Shutdown();
                    return;
                }

                // Initialize database
                InitializeDatabase();

                // Show login window first
                ShowLoginWindow();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Application startup error:\n\n{ex.Message}\n\nStack trace:\n{ex.StackTrace}",
                    "Startup Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown();
            }
        }

        private void ConfigureServices(IServiceCollection services)
        {
            // Database
            var dbPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "InvoiceApp",
                "invoice.db");

            Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);

            services.AddDbContext<InvoiceDbContext>(options =>
                options.UseSqlite($"Data Source={dbPath}"));

            // Services
            services.AddScoped<IProductService, ProductService>();
            services.AddScoped<IClientService, ClientService>();
            services.AddScoped<IInvoiceService, InvoiceService>();
            services.AddScoped<IStatementService, StatementService>();
            services.AddScoped<ISettingsService, SettingsService>();
            services.AddScoped<IPdfService, PdfService>();
            services.AddScoped<IEmailService, EmailService>();
            services.AddSingleton<IAuthenticationService, AuthenticationService>();

            // Views and ViewModels
            services.AddTransient<MainWindow>();
            services.AddTransient<MainWindowViewModel>();

            services.AddSingleton<IUpdateService, UpdateService>();
        }

        private void InitializeDatabase()
        {
            using var scope = _serviceProvider!.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<InvoiceDbContext>();
            context.Database.EnsureCreated();
        }

        public void ShowLoginWindow()
        {
            if (_serviceProvider == null)
            {
                MessageBox.Show("Service provider is not initialized.", "Error");
                Shutdown();
                return;
            }

            try
            {
                var authService = _serviceProvider.GetRequiredService<IAuthenticationService>();
                var loginWindow = new LoginWindow(authService);

                // Show login as dialog - this BLOCKS until closed
                var loginResult = loginWindow.ShowDialog();

                System.Diagnostics.Debug.WriteLine($"Login dialog result: {loginResult}");

                if (loginResult == true)
                {
                    // Login successful
                    System.Diagnostics.Debug.WriteLine("Login successful, showing main window...");

                    // IMPORTANT: LoginWindow is now closed, we can safely show MainWindow
                    ShowMainWindow();
                }
                else
                {
                    // Login cancelled
                    System.Diagnostics.Debug.WriteLine("Login cancelled");
                    Shutdown();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Login error: {ex.Message}");
                MessageBox.Show($"Login error:\n\n{ex.Message}\n\n{ex.StackTrace}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown();
            }
        }

        private void ShowMainWindow()
        {
            System.Diagnostics.Debug.WriteLine("=== ShowMainWindow START ===");

            if (_serviceProvider == null)
            {
                System.Diagnostics.Debug.WriteLine("ERROR: _serviceProvider is NULL");
                MessageBox.Show("Service provider is not initialized.", "Error");
                Shutdown();
                return;
            }

            try
            {
                System.Diagnostics.Debug.WriteLine("Creating MainWindow...");
                var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();

                if (mainWindow == null)
                {
                    System.Diagnostics.Debug.WriteLine("ERROR: MainWindow is NULL");
                    MessageBox.Show("Failed to create main window.", "Error");
                    Shutdown();
                    return;
                }

                System.Diagnostics.Debug.WriteLine("MainWindow created successfully");

                // Set as main window
                Application.Current.MainWindow = mainWindow;

                System.Diagnostics.Debug.WriteLine("MainWindow set");

                // Handle window closing - shutdown when main window closes
                mainWindow.Closed += (s, e) =>
                {
                    System.Diagnostics.Debug.WriteLine("MainWindow closed - shutting down app");
                    Shutdown();
                };

                // Show the window
                mainWindow.Show();

                System.Diagnostics.Debug.WriteLine($"After Show() - IsVisible: {mainWindow.IsVisible}, IsLoaded: {mainWindow.IsLoaded}");

                // Force activation
                mainWindow.Activate();

                System.Diagnostics.Debug.WriteLine("=== ShowMainWindow END ===");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"EXCEPTION in ShowMainWindow: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                MessageBox.Show($"Error loading main window:\n\n{ex.Message}\n\nInner: {ex.InnerException?.Message}\n\nStack:\n{ex.StackTrace}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown();
            }
        }
    }
}