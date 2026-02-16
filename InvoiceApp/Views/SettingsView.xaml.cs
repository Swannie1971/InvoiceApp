using InvoiceApp.Helpers;
using InvoiceApp.Services;
using InvoiceApp.ViewModels;
using System;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;

namespace InvoiceApp.Views
{
    public partial class SettingsView : UserControl
    {
        public SettingsView()
        {
            InitializeComponent();
            VersionText.Text = AppVersion.Version;
            Loaded += SettingsView_Loaded;
        }

        private void SettingsView_Loaded(object sender, RoutedEventArgs e)
        {
            // Load password when view loads
            if (DataContext is SettingsViewModel viewModel && viewModel.Settings != null)
            {
                SmtpPasswordBox.Password = viewModel.Settings.SmtpPassword ?? string.Empty;
            }
        }

        private void SmtpPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            // Update the ViewModel when password changes
            if (DataContext is SettingsViewModel viewModel && viewModel.Settings != null)
            {
                viewModel.Settings.SmtpPassword = SmtpPasswordBox.Password;
            }
        }
        private async void TestButton_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is SettingsViewModel viewModel)
            {
                await viewModel.CheckForUpdatesAsync();
            }
        }
        private void ChangePasswordButton_Click(object sender, RoutedEventArgs e)
        {
            var app = (App)Application.Current;
            var authService = app.Services.GetRequiredService<IAuthenticationService>();

            var dialog = new ChangePasswordDialog(authService);
            dialog.ShowDialog();
        }
    }
}