using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using InvoiceApp.Models;
using InvoiceApp.Services;
using Microsoft.Win32;

namespace InvoiceApp.ViewModels
{
    public class SettingsViewModel : ViewModelBase
    {
        private readonly ISettingsService _settingsService;
        private readonly IUpdateService _updateService;
        private AppSettings? _settings;

        public SettingsViewModel(ISettingsService settingsService, IUpdateService updateService)
        {
            _settingsService = settingsService;
            _updateService = updateService;

            SaveCommand = new AsyncRelayCommand(SaveSettingsAsync);
            BrowseLogoCommand = new RelayCommand(BrowseLogo);
            TestEmailCommand = new AsyncRelayCommand(TestEmailAsync);
            CheckUpdatesCommand = new AsyncRelayCommand(CheckForUpdatesAsync);

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
        public ICommand CheckUpdatesCommand { get; }

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

        public async Task CheckForUpdatesAsync()
        {
            try
            {
                var updateInfo = await _updateService.CheckForUpdatesAsync();

                if (updateInfo.IsUpdateAvailable)
                {
                    await _updateService.DownloadAndInstallUpdateAsync(updateInfo);
                }
                else
                {
                    MessageBox.Show(
                        $"You are running the latest version ({updateInfo.CurrentVersion}).",
                        "No Updates Available",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error checking for updates: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}