using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using System;
using InvoiceApp.Models;
using InvoiceApp.Services;
using System.Collections.ObjectModel;

namespace InvoiceApp.ViewModels
{
    public class DashboardViewModel : ViewModelBase
    {
        private readonly IInvoiceService _invoiceService;
        private readonly IClientService _clientService;

        private int _totalClients;
        private int _totalInvoices;
        private decimal _totalRevenue;
        private int _overdueInvoices;
        private ObservableCollection<Invoice> _recentInvoices = new();
        private int _pendingInvoices;
        private decimal _thisMonthRevenue;
        private decimal _averageInvoice;

        public int PendingInvoices
        {
            get => _pendingInvoices;
            set => SetProperty(ref _pendingInvoices, value);
        }

        public decimal ThisMonthRevenue
        {
            get => _thisMonthRevenue;
            set => SetProperty(ref _thisMonthRevenue, value);
        }

        public decimal AverageInvoice
        {
            get => _averageInvoice;
            set => SetProperty(ref _averageInvoice, value);
        }
        public DashboardViewModel(IInvoiceService invoiceService, IClientService clientService)
        {
            _invoiceService = invoiceService;
            _clientService = clientService;

            LoadDashboardDataAsync();
        }

        public int TotalClients
        {
            get => _totalClients;
            set => SetProperty(ref _totalClients, value);
        }

        public int TotalInvoices
        {
            get => _totalInvoices;
            set => SetProperty(ref _totalInvoices, value);
        }

        public decimal TotalRevenue
        {
            get => _totalRevenue;
            set => SetProperty(ref _totalRevenue, value);
        }

        public int OverdueInvoices
        {
            get => _overdueInvoices;
            set => SetProperty(ref _overdueInvoices, value);
        }

        public ObservableCollection<Invoice> RecentInvoices
        {
            get => _recentInvoices;
            set => SetProperty(ref _recentInvoices, value);
        }

        private async void LoadDashboardDataAsync()
        {
            var clients = await _clientService.GetActiveClientsAsync();
            TotalClients = clients.Count;

            var invoices = await _invoiceService.GetAllInvoicesAsync();
            TotalInvoices = invoices.Count;

            TotalRevenue = invoices.Where(i => i.Status == InvoiceStatus.Paid).Sum(i => i.Total);

            var overdueInvoices = await _invoiceService.GetOverdueInvoicesAsync();
            OverdueInvoices = overdueInvoices.Count;

            // Pending invoices (Draft or Sent)
            PendingInvoices = invoices.Count(i => i.Status == InvoiceStatus.Draft || i.Status == InvoiceStatus.Sent);

            // This month revenue
            var thisMonth = DateTime.Now.Month;
            var thisYear = DateTime.Now.Year;
            ThisMonthRevenue = invoices
                .Where(i => i.InvoiceDate.Month == thisMonth && i.InvoiceDate.Year == thisYear && i.Status == InvoiceStatus.Paid)
                .Sum(i => i.Total);

            // Average invoice
            AverageInvoice = TotalInvoices > 0 ? invoices.Average(i => i.Total) : 0;

            // Recent invoices
            RecentInvoices = new ObservableCollection<Invoice>(invoices.OrderByDescending(i => i.InvoiceDate).Take(10));
        }
    }
}


