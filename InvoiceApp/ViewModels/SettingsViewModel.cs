using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using System;
using InvoiceApp.Models;
using InvoiceApp.Services;
using Microsoft.Win32;
using System.Windows;
using System.Windows.Input;
using System.IO;

namespace InvoiceApp.ViewModels
{
    public class SettingsViewModel : ViewModelBase
    {
        private readonly ISettingsService _settingsService;
        private AppSettings? _settings;

        public SettingsViewModel(ISettingsService settingsService)
        {
            _settingsService = settingsService;

            SaveCommand = new AsyncRelayCommand(SaveSettingsAsync);
            BrowseLogoCommand = new RelayCommand(BrowseLogo);
            TestEmailCommand = new AsyncRelayCommand(TestEmailAsync);

            LoadSettingsAsync();
        }

        public AppSettings? Settings
        {
            get => _settings;
            set => SetProperty(ref _settings, value);
        }

        public ICommand SaveCommand { get; }
        public ICommand BrowseLogoCommand { get; }
        public ICommand TestEmailCommand { get; }

        private async void LoadSettingsAsync()
        {
            Settings = await _settingsService.GetSettingsAsync();
        }

        private async Task SaveSettingsAsync()
        {
            if (Settings != null)
            {
                await _settingsService.UpdateSettingsAsync(Settings);
                MessageBox.Show("Settings saved successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void BrowseLogo()
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Image Files|*.png;*.jpg;*.jpeg;*.bmp|All Files|*.*",
                Title = "Select Company Logo"
            };

            if (dialog.ShowDialog() == true && Settings != null)
            {
                Settings.CompanyLogo = File.ReadAllBytes(dialog.FileName);
                OnPropertyChanged(nameof(Settings));
                MessageBox.Show("Logo loaded successfully!");
            }
        }

        private async Task TestEmailAsync()
        {
            if (Settings != null)
            {
                try
                {
                    var emailService = new EmailService(null!, _settingsService);
                    var success = await emailService.TestEmailConnectionAsync(Settings);

                    if (success)
                    {
                        MessageBox.Show("Email test successful! Check your inbox.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show("Email test failed. Please check your settings.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                catch (Exception ex)
                {
                    var innerMessage = ex.InnerException?.Message ?? "No additional details";
                    MessageBox.Show($"Email test failed:\n\n{ex.Message}\n\nDetails:\n{innerMessage}\n\nSettings:\nServer: {Settings.SmtpServer}\nPort: {Settings.SmtpPort}\nUsername: {Settings.SmtpUsername}\nSSL: {Settings.SmtpUseSsl}",
                        "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}


