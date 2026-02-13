using System;
using System.Windows;
using System.Windows.Input;
using InvoiceApp.Services;

namespace InvoiceApp.Views
{
    public partial class LoginWindow : Window
    {
        private readonly IAuthenticationService _authService;

        public LoginWindow(IAuthenticationService authService)
        {
            InitializeComponent();
            _authService = authService;

            // Set focus to username
            Loaded += (s, e) => UsernameTextBox.Focus();

            // Allow Enter key to login
            UsernameTextBox.KeyDown += (s, e) => { if (e.Key == Key.Enter) PasswordBox.Focus(); };
            PasswordBox.KeyDown += (s, e) => { if (e.Key == Key.Enter) LoginButton_Click(s, e); };
        }

        private async void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            ErrorText.Visibility = Visibility.Collapsed;

            var username = UsernameTextBox.Text.Trim();
            var password = PasswordBox.Password;

            if (string.IsNullOrWhiteSpace(username))
            {
                ShowError("Please enter your username.");
                UsernameTextBox.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                ShowError("Please enter your password.");
                PasswordBox.Focus();
                return;
            }

            try
            {
                var user = await _authService.LoginAsync(username, password);

                if (user == null)
                {
                    ShowError("Invalid username or password.");
                    PasswordBox.Clear();
                    PasswordBox.Focus();
                    return;
                }

                // Login successful
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                ShowError($"Login failed: {ex.Message}");
            }
        }

        private void ShowError(string message)
        {
            ErrorText.Text = message;
            ErrorText.Visibility = Visibility.Visible;
        }
    }
}