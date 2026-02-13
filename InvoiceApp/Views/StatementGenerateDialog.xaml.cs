using System;
using System.Linq;
using System.Windows;
using InvoiceApp.Services;

namespace InvoiceApp.Views
{
    public partial class StatementGenerateDialog : Window
    {
        private readonly IStatementService _statementService;
        private readonly IClientService _clientService;

        public StatementGenerateDialog(IStatementService statementService, IClientService clientService)
        {
            InitializeComponent();
            _statementService = statementService;
            _clientService = clientService;

            LoadClientsAsync();

            // Set default dates
            StartDatePicker.SelectedDate = DateTime.Today.AddMonths(-1);
            EndDatePicker.SelectedDate = DateTime.Today;
        }

        private async void LoadClientsAsync()
        {
            var clients = await _clientService.GetActiveClientsAsync();
            ClientComboBox.ItemsSource = clients;
            if (clients.Any())
            {
                ClientComboBox.SelectedIndex = 0;
            }
        }

        private async void GenerateButton_Click(object sender, RoutedEventArgs e)
        {
            if (ClientComboBox.SelectedValue == null)
            {
                MessageBox.Show("Please select a client.", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!StartDatePicker.SelectedDate.HasValue || !EndDatePicker.SelectedDate.HasValue)
            {
                MessageBox.Show("Please select start and end dates.", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (StartDatePicker.SelectedDate.Value > EndDatePicker.SelectedDate.Value)
            {
                MessageBox.Show("Start date must be before end date.", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!decimal.TryParse(OpeningBalanceTextBox.Text, out decimal openingBalance))
            {
                MessageBox.Show("Please enter a valid opening balance.", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                int clientId = (int)ClientComboBox.SelectedValue;
                await _statementService.GenerateStatementAsync(
                    clientId,
                    StartDatePicker.SelectedDate.Value,
                    EndDatePicker.SelectedDate.Value,
                    openingBalance);

                MessageBox.Show("Statement generated successfully!", "Success",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error generating statement: {ex.Message}", "Error",
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