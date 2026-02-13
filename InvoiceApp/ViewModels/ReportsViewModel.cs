using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using InvoiceApp.Models;
using InvoiceApp.Services;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using Microsoft.Win32;
using ClosedXML.Excel;

namespace InvoiceApp.ViewModels
{
    public class ReportsViewModel : ViewModelBase
    {
        private readonly IInvoiceService _invoiceService;
        private readonly IClientService _clientService;
        private readonly ISettingsService _settingsService;
        private string _currencySymbol = "$";

        // Revenue Chart Model
        private PlotModel _revenueChartModel;

        // Top Clients Data
        private List<ClientRevenueItem> _topClients;

        // Aging Report Data
        private List<AgingReportItem> _agingReport;

        // Payment Summary
        private decimal _totalRevenue;
        private decimal _totalPaid;
        private decimal _totalOutstanding;
        private int _totalInvoices;
        private int _paidInvoices;
        private int _overdueInvoices;

        public ReportsViewModel(
    IInvoiceService invoiceService,
    IClientService clientService,
    ISettingsService settingsService)
        {
            _invoiceService = invoiceService;
            _clientService = clientService;
            _settingsService = settingsService;

            RefreshCommand = new AsyncRelayCommand(LoadReportsAsync);
            ExportToExcelCommand = new AsyncRelayCommand(ExportToExcelAsync);

            // Load data asynchronously
            _ = LoadReportsAsync();
        }

        // Chart Property
        public PlotModel RevenueChartModel
        {
            get => _revenueChartModel;
            set => SetProperty(ref _revenueChartModel, value);
        }
        private PlotModel _revenueBreakdownModel;
        public PlotModel RevenueBreakdownModel
        {
            get => _revenueBreakdownModel;
            set => SetProperty(ref _revenueBreakdownModel, value);
        }
        // Top Clients
        public List<ClientRevenueItem> TopClients
        {
            get => _topClients;
            set => SetProperty(ref _topClients, value);
        }

        // Aging Report
        public List<AgingReportItem> AgingReport
        {
            get => _agingReport;
            set => SetProperty(ref _agingReport, value);
        }

        // Summary Stats
        public decimal TotalRevenue
        {
            get => _totalRevenue;
            set => SetProperty(ref _totalRevenue, value);
        }

        public decimal TotalPaid
        {
            get => _totalPaid;
            set => SetProperty(ref _totalPaid, value);
        }

        public decimal TotalOutstanding
        {
            get => _totalOutstanding;
            set => SetProperty(ref _totalOutstanding, value);
        }

        public int TotalInvoices
        {
            get => _totalInvoices;
            set => SetProperty(ref _totalInvoices, value);
        }

        public int PaidInvoices
        {
            get => _paidInvoices;
            set => SetProperty(ref _paidInvoices, value);
        }

        public int OverdueInvoices
        {
            get => _overdueInvoices;
            set => SetProperty(ref _overdueInvoices, value);
        }

        public ICommand RefreshCommand { get; }
        public ICommand ExportToExcelCommand { get; }

        private async Task LoadReportsAsync()
        {
            var invoices = await _invoiceService.GetAllInvoicesAsync();
            var settings = await _settingsService.GetSettingsAsync();
            _currencySymbol = settings.CurrencySymbol;

            System.Diagnostics.Debug.WriteLine($"=== REPORTS: Loaded {invoices.Count} invoices ===");

            // 1. Revenue Summary
            TotalRevenue = invoices.Sum(i => i.Total);
            TotalPaid = invoices.Sum(i => i.AmountPaid);
            TotalOutstanding = invoices.Sum(i => i.AmountRemaining);
            TotalInvoices = invoices.Count;
            PaidInvoices = invoices.Count(i => i.Status == InvoiceStatus.Paid);
            OverdueInvoices = invoices.Count(i => i.Status == InvoiceStatus.Overdue);

            System.Diagnostics.Debug.WriteLine($"Total Revenue: {TotalRevenue}");
            System.Diagnostics.Debug.WriteLine($"Total Paid: {TotalPaid}");
            System.Diagnostics.Debug.WriteLine($"Total Outstanding: {TotalOutstanding}");

            // 2. Revenue by Month Chart (Last 12 months)
            LoadRevenueChart(invoices);

            // 2.5 Revenue Breakdown Pie Chart ← ADD THIS
            LoadRevenueBreakdownChart();

            // 3. Top 10 Clients by Revenue
            LoadTopClients(invoices);

            // 4. Aging Report
            LoadAgingReport(invoices);

            System.Diagnostics.Debug.WriteLine($"=== REPORTS: Loading complete ===");
        }

        private void LoadRevenueChart(List<Invoice> invoices)
        {
            var plotModel = new PlotModel
            {
                Background = OxyColors.Transparent,
                PlotAreaBorderThickness = new OxyThickness(0),
                Padding = new OxyThickness(40, 10, 40, 40)
            };

            var categoryAxis = new CategoryAxis
            {
                Position = AxisPosition.Left,
                Key = "Category",
                TickStyle = TickStyle.None,
                TextColor = OxyColors.DarkGray,
                FontSize = 11
            };

            var valueAxis = new LinearAxis
            {
                Position = AxisPosition.Bottom,
                MinimumPadding = 0.1,
                MaximumPadding = 0.1,
                TickStyle = TickStyle.None,
                MajorGridlineStyle = LineStyle.Solid,
                MajorGridlineColor = OxyColor.FromRgb(240, 240, 240),
                TextColor = OxyColors.DarkGray,
                FontSize = 11,
                StringFormat = _currencySymbol + "#,0"
            };

            plotModel.Axes.Add(categoryAxis);
            plotModel.Axes.Add(valueAxis);

            var barSeries = new BarSeries
            {
                FillColor = OxyColor.FromRgb(52, 152, 219),
                StrokeThickness = 0,
                BarWidth = 15,
                LabelPlacement = LabelPlacement.Outside,
                LabelFormatString = _currencySymbol + "{0:N0}",
                TextColor = OxyColors.DarkGray,
                FontSize = 10
            };

            // Get last 12 months
            var today = DateTime.Today;
            for (int i = 11; i >= 0; i--)
            {
                var month = today.AddMonths(-i);
                categoryAxis.Labels.Add(month.ToString("MMM yyyy"));

                var monthRevenue = invoices
                    .Where(inv => inv.InvoiceDate.Year == month.Year && inv.InvoiceDate.Month == month.Month)
                    .Sum(inv => inv.AmountPaid);

                barSeries.Items.Add(new BarItem((double)monthRevenue));
            }

            plotModel.Series.Add(barSeries);
            RevenueChartModel = plotModel;
        }
        private void LoadRevenueBreakdownChart()
        {
            var plotModel = new PlotModel
            {
                Background = OxyColors.Transparent,
                PlotAreaBorderThickness = new OxyThickness(0),
                Padding = new OxyThickness(80, 20, 80, 20)
            };

            var pieSeries = new PieSeries
            {
                StrokeThickness = 3,
                Stroke = OxyColors.White,
                AngleSpan = 360,
                StartAngle = 0,
                FontSize = 11,
                TextColor = OxyColors.Black,
                OutsideLabelFormat = null // We set labels directly on slices
            };

            if (TotalPaid > 0)
            {
                var amount = TotalPaid.ToString("N2").Replace(",", " ");
                var slice = new PieSlice($"Paid: {_currencySymbol}{amount}", (double)TotalPaid);
                slice.Fill = OxyColor.FromRgb(46, 204, 113);
                slice.IsExploded = false;
                pieSeries.Slices.Add(slice);
            }

            if (TotalOutstanding > 0)
            {
                var amount = TotalOutstanding.ToString("N2").Replace(",", " ");
                var slice = new PieSlice($"Outstanding: {_currencySymbol}{amount}", (double)TotalOutstanding);
                slice.Fill = OxyColor.FromRgb(231, 76, 60);
                slice.IsExploded = true;
                pieSeries.Slices.Add(slice);
            }

            plotModel.Series.Add(pieSeries);
            RevenueBreakdownModel = plotModel;
        }
        private void LoadTopClients(List<Invoice> invoices)
        {
            var clientRevenue = invoices
                .GroupBy(i => i.Client)
                .Select(g => new ClientRevenueItem
                {
                    ClientName = g.Key.CompanyName,
                    TotalRevenue = g.Sum(i => i.Total),
                    PaidAmount = g.Sum(i => i.AmountPaid),
                    InvoiceCount = g.Count()
                })
                .OrderByDescending(c => c.TotalRevenue)
                .Take(10)
                .ToList();

            TopClients = clientRevenue;
        }

        private void LoadAgingReport(List<Invoice> invoices)
        {
            var today = DateTime.Today;
            var unpaidInvoices = invoices.Where(i => i.AmountRemaining > 0 && i.Status != InvoiceStatus.Draft).ToList();

            var aging = new List<AgingReportItem>
            {
                new AgingReportItem
                {
                    Category = "Current (Not Due)",
                    Count = unpaidInvoices.Count(i => i.DueDate >= today),
                    Amount = unpaidInvoices.Where(i => i.DueDate >= today).Sum(i => i.AmountRemaining)
                },
                new AgingReportItem
                {
                    Category = "1-30 Days Overdue",
                    Count = unpaidInvoices.Count(i => i.DueDate < today && i.DueDate >= today.AddDays(-30)),
                    Amount = unpaidInvoices.Where(i => i.DueDate < today && i.DueDate >= today.AddDays(-30)).Sum(i => i.AmountRemaining)
                },
                new AgingReportItem
                {
                    Category = "31-60 Days Overdue",
                    Count = unpaidInvoices.Count(i => i.DueDate < today.AddDays(-30) && i.DueDate >= today.AddDays(-60)),
                    Amount = unpaidInvoices.Where(i => i.DueDate < today.AddDays(-30) && i.DueDate >= today.AddDays(-60)).Sum(i => i.AmountRemaining)
                },
                new AgingReportItem
                {
                    Category = "61-90 Days Overdue",
                    Count = unpaidInvoices.Count(i => i.DueDate < today.AddDays(-60) && i.DueDate >= today.AddDays(-90)),
                    Amount = unpaidInvoices.Where(i => i.DueDate < today.AddDays(-60) && i.DueDate >= today.AddDays(-90)).Sum(i => i.AmountRemaining)
                },
                new AgingReportItem
                {
                    Category = "90+ Days Overdue",
                    Count = unpaidInvoices.Count(i => i.DueDate < today.AddDays(-90)),
                    Amount = unpaidInvoices.Where(i => i.DueDate < today.AddDays(-90)).Sum(i => i.AmountRemaining)
                }
            };

            AgingReport = aging;
        }

        private async Task ExportToExcelAsync()
        {
            try
            {
                var saveDialog = new SaveFileDialog
                {
                    Filter = "Excel Files (*.xlsx)|*.xlsx",
                    FileName = $"Reports_{DateTime.Now:yyyyMMdd}.xlsx",
                    DefaultExt = ".xlsx"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    using var workbook = new XLWorkbook();

                    // Summary Sheet
                    var summarySheet = workbook.Worksheets.Add("Summary");
                    summarySheet.Cell(1, 1).Value = "Metric";
                    summarySheet.Cell(1, 2).Value = "Value";
                    summarySheet.Cell(2, 1).Value = "Total Revenue";
                    summarySheet.Cell(2, 2).Value = TotalRevenue;
                    summarySheet.Cell(3, 1).Value = "Total Paid";
                    summarySheet.Cell(3, 2).Value = TotalPaid;
                    summarySheet.Cell(4, 1).Value = "Total Outstanding";
                    summarySheet.Cell(4, 2).Value = TotalOutstanding;
                    summarySheet.Cell(5, 1).Value = "Total Invoices";
                    summarySheet.Cell(5, 2).Value = TotalInvoices;
                    summarySheet.Cell(6, 1).Value = "Paid Invoices";
                    summarySheet.Cell(6, 2).Value = PaidInvoices;
                    summarySheet.Cell(7, 1).Value = "Overdue Invoices";
                    summarySheet.Cell(7, 2).Value = OverdueInvoices;

                    // Top Clients Sheet
                    var clientsSheet = workbook.Worksheets.Add("Top Clients");
                    clientsSheet.Cell(1, 1).Value = "Client";
                    clientsSheet.Cell(1, 2).Value = "Total Revenue";
                    clientsSheet.Cell(1, 3).Value = "Paid Amount";
                    clientsSheet.Cell(1, 4).Value = "Invoice Count";

                    int row = 2;
                    foreach (var client in TopClients)
                    {
                        clientsSheet.Cell(row, 1).Value = client.ClientName;
                        clientsSheet.Cell(row, 2).Value = client.TotalRevenue;
                        clientsSheet.Cell(row, 3).Value = client.PaidAmount;
                        clientsSheet.Cell(row, 4).Value = client.InvoiceCount;
                        row++;
                    }

                    // Aging Report Sheet
                    var agingSheet = workbook.Worksheets.Add("Aging Report");
                    agingSheet.Cell(1, 1).Value = "Category";
                    agingSheet.Cell(1, 2).Value = "Count";
                    agingSheet.Cell(1, 3).Value = "Amount";

                    row = 2;
                    foreach (var item in AgingReport)
                    {
                        agingSheet.Cell(row, 1).Value = item.Category;
                        agingSheet.Cell(row, 2).Value = item.Count;
                        agingSheet.Cell(row, 3).Value = item.Amount;
                        row++;
                    }

                    workbook.SaveAs(saveDialog.FileName);

                    MessageBox.Show($"Report exported successfully to:\n{saveDialog.FileName}", "Success",
                        MessageBoxButton.OK, MessageBoxImage.Information);

                    // Ask if they want to open it
                    var result = MessageBox.Show("Would you like to open the file now?", "Open File",
                        MessageBoxButton.YesNo, MessageBoxImage.Question);

                    if (result == MessageBoxResult.Yes)
                    {
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = saveDialog.FileName,
                            UseShellExecute = true
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error exporting to Excel: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }

            await Task.CompletedTask;
        }
    }

    // Helper classes
    public class ClientRevenueItem
    {
        public string ClientName { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal PaidAmount { get; set; }
        public int InvoiceCount { get; set; }
    }

    public class AgingReportItem
    {
        public string Category { get; set; }
        public int Count { get; set; }
        public decimal Amount { get; set; }
    }
}