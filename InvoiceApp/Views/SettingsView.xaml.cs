using System;
using System.Windows;
using System.Windows.Controls;
using InvoiceApp.ViewModels;

namespace InvoiceApp.Views
{
    public partial class SettingsView : UserControl
    {
        public SettingsView()
        {
            InitializeComponent();
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
    }
}


