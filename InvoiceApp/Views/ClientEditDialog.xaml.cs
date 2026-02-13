using System;
using System.Windows;
using InvoiceApp.Models;
using InvoiceApp.Services;

namespace InvoiceApp.Views
{
    public partial class ClientEditDialog : Window
    {
        private readonly IClientService _clientService;
        private readonly int? _clientId;
        private Client? _client;

        public ClientEditDialog(IClientService clientService, int? clientId = null)
        {
            InitializeComponent();
            _clientService = clientService;
            _clientId = clientId;

            if (_clientId.HasValue)
            {
                LoadClientAsync();
            }
        }

        private async void LoadClientAsync()
        {
            _client = await _clientService.GetClientByIdAsync(_clientId!.Value);
            if (_client != null)
            {
                CompanyNameTextBox.Text = _client.CompanyName;
                ContactPersonTextBox.Text = _client.ContactPerson;
                EmailTextBox.Text = _client.Email;
                PhoneTextBox.Text = _client.Phone;
                BillingAddressTextBox.Text = _client.BillingAddress;
                VatNumberTextBox.Text = _client.VatNumber;
                IsActiveCheckBox.IsChecked = _client.IsActive;

                Title = "Edit Client";
            }
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            // Validate required fields
            if (string.IsNullOrWhiteSpace(CompanyNameTextBox.Text))
            {
                MessageBox.Show("Company name is required.", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                CompanyNameTextBox.Focus();
                return;
            }

            try
            {
                if (_client == null)
                {
                    _client = new Client();
                }

                _client.CompanyName = CompanyNameTextBox.Text.Trim();
                _client.ContactPerson = ContactPersonTextBox.Text.Trim();
                _client.Email = EmailTextBox.Text.Trim();
                _client.Phone = PhoneTextBox.Text.Trim();
                _client.BillingAddress = BillingAddressTextBox.Text.Trim();
                _client.VatNumber = VatNumberTextBox.Text.Trim();
                _client.IsActive = IsActiveCheckBox.IsChecked ?? true;

                if (_clientId.HasValue)
                {
                    await _clientService.UpdateClientAsync(_client);
                    MessageBox.Show("Client updated successfully!", "Success",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    await _clientService.CreateClientAsync(_client);
                    MessageBox.Show("Client created successfully!", "Success",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving client: {ex.Message}", "Error",
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