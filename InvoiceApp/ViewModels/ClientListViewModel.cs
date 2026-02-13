using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using System;
using InvoiceApp.Models;
using InvoiceApp.Services;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;

namespace InvoiceApp.ViewModels
{
    public class ClientListViewModel : ViewModelBase
    {
        private readonly IClientService _clientService;
        private ObservableCollection<Client> _clients = new();
        private Client? _selectedClient;

        public ClientListViewModel(IClientService clientService)
        {
            _clientService = clientService;

            CreateClientCommand = new AsyncRelayCommand(CreateClientAsync);
            EditClientCommand = new RelayCommand(EditClient, () => SelectedClient != null);
            DeleteClientCommand = new AsyncRelayCommand(DeleteClientAsync, () => SelectedClient != null);
            RefreshCommand = new AsyncRelayCommand(LoadClientsAsync);

            LoadClientsAsync();
        }

        public ObservableCollection<Client> Clients
        {
            get => _clients;
            set => SetProperty(ref _clients, value);
        }

        public Client? SelectedClient
        {
            get => _selectedClient;
            set
            {
                if (SetProperty(ref _selectedClient, value))
                {
                    ((RelayCommand)EditClientCommand).RaiseCanExecuteChanged();
                    ((AsyncRelayCommand)DeleteClientCommand).RaiseCanExecuteChanged();
                }
            }
        }

        public ICommand CreateClientCommand { get; }
        public ICommand EditClientCommand { get; }
        public ICommand DeleteClientCommand { get; }
        public ICommand RefreshCommand { get; }

        private async Task LoadClientsAsync()
        {
            var clients = await _clientService.GetAllClientsAsync();
            Clients = new ObservableCollection<Client>(clients);
        }

        private async Task CreateClientAsync()
        {
            var dialog = new Views.ClientEditDialog(_clientService);
            if (dialog.ShowDialog() == true)
            {
                await LoadClientsAsync();
            }
        }

        private void EditClient()
        {
            if (SelectedClient != null)
            {
                var dialog = new Views.ClientEditDialog(_clientService, SelectedClient.Id);
                if (dialog.ShowDialog() == true)
                {
                    LoadClientsAsync();
                }
            }
        }

        private async Task DeleteClientAsync()
        {
            if (SelectedClient != null)
            {
                var hasInvoices = await _clientService.HasInvoicesAsync(SelectedClient.Id);
                if (hasInvoices)
                {
                    MessageBox.Show(
                        "This client has invoices and cannot be deleted. The client will be marked as inactive.",
                        "Cannot Delete",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }

                await _clientService.DeleteClientAsync(SelectedClient.Id);
                await LoadClientsAsync();
            }
        }
    }
}


