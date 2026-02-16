using System;
using System.Windows;
using InvoiceApp.Services;

namespace InvoiceApp.Views
{
    public partial class ChangePasswordDialog : Window
    {
        private readonly IAuthenticationService _authService;

        public ChangePasswordDialog(IAuthenticationService authService)
        {
            InitializeComponent();
            _authService = authService;
        }

        private async void ChangeButton_Click(object sender, RoutedEventArgs e)
        {
            // Validate inputs
            if (string.IsNullOrWhiteSpace(CurrentPasswordBox.Password))
            {
                MessageBox.Show("Please enter your current password.", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                CurrentPasswordBox.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(NewPasswordBox.Password))
            {
                MessageBox.Show("Please enter a new password.", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                NewPasswordBox.Focus();
                return;
            }

            if (NewPasswordBox.Password.Length < 6)
            {
                MessageBox.Show("Password must be at least 6 characters long.", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                NewPasswordBox.Focus();
                return;
            }

            if (NewPasswordBox.Password != ConfirmPasswordBox.Password)
            {
                MessageBox.Show("New passwords do not match.", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                ConfirmPasswordBox.Focus();
                return;
            }

            try
            {
                var currentUser = _authService.CurrentUser;
                if (currentUser == null)
                {
                    MessageBox.Show("No user is currently logged in.", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var success = await _authService.ChangePasswordAsync(
                    currentUser.Id,
                    CurrentPasswordBox.Password,
                    NewPasswordBox.Password);

                if (success)
                {
                    MessageBox.Show("Password changed successfully!", "Success",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    DialogResult = true;
                    Close();
                }
                else
                {
                    MessageBox.Show("Current password is incorrect.", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    CurrentPasswordBox.Clear();
                    CurrentPasswordBox.Focus();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error changing password: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}