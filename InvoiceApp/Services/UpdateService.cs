using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using InvoiceApp.Helpers;

namespace InvoiceApp.Services
{
    public class UpdateService : IUpdateService
    {
        private const string UPDATE_CHECK_URL = "https://raw.githubusercontent.com/Swannie1971/InvoiceApp/main/version.json";
        private readonly HttpClient _httpClient;

        public UpdateService()
        {
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "InvoiceApp-Updater");
        }

        public async Task<UpdateInfo> CheckForUpdatesAsync()
        {
            try
            {
                var response = await _httpClient.GetStringAsync(UPDATE_CHECK_URL);
                var versionData = JsonSerializer.Deserialize<VersionData>(response);

                if (versionData == null)
                {
                    return new UpdateInfo
                    {
                        IsUpdateAvailable = false,
                        CurrentVersion = AppVersion.Version
                    };
                }

                var currentVersion = new Version(AppVersion.Version);
                var latestVersion = new Version(versionData.Version);

                System.Diagnostics.Debug.WriteLine($"Current Version: {currentVersion}");
                System.Diagnostics.Debug.WriteLine($"Latest Version: {latestVersion}");
                System.Diagnostics.Debug.WriteLine($"Is Update Available: {latestVersion > currentVersion}");


                return new UpdateInfo
                {
                    IsUpdateAvailable = latestVersion > currentVersion,
                    LatestVersion = versionData.Version,
                    CurrentVersion = AppVersion.Version,
                    DownloadUrl = versionData.DownloadUrl,
                    ReleaseNotes = versionData.ReleaseNotes,
                    ReleaseDate = DateTime.Parse(versionData.ReleaseDate)
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Update check failed: {ex.Message}");
                return new UpdateInfo
                {
                    IsUpdateAvailable = false,
                    CurrentVersion = AppVersion.Version
                };
            }
        }

        public async Task<bool> DownloadAndInstallUpdateAsync(UpdateInfo updateInfo)
        {
            try
            {
                var result = MessageBox.Show(
                    $"A new version is available!\n\n" +
                    $"Current Version: {updateInfo.CurrentVersion}\n" +
                    $"Latest Version: {updateInfo.LatestVersion}\n\n" +
                    $"Release Notes:\n{updateInfo.ReleaseNotes}\n\n" +
                    $"Would you like to download and install the update now?",
                    "Update Available",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Information);

                if (result != MessageBoxResult.Yes)
                    return false;

                // Download the installer
                var installerPath = Path.Combine(Path.GetTempPath(), "InvoiceApp_Update.exe");

                var installerBytes = await _httpClient.GetByteArrayAsync(updateInfo.DownloadUrl);
                await File.WriteAllBytesAsync(installerPath, installerBytes);

                // Start the installer
                Process.Start(new ProcessStartInfo
                {
                    FileName = installerPath,
                    UseShellExecute = true
                });

                // Close the current app
                Application.Current.Shutdown();
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Update failed: {ex.Message}", "Update Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        private class VersionData
        {
            public string Version { get; set; } = string.Empty;
            public string DownloadUrl { get; set; } = string.Empty;
            public string ReleaseNotes { get; set; } = string.Empty;
            public string ReleaseDate { get; set; } = string.Empty;
        }
    }
}