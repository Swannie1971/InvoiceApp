# Invoice App - Implementation Guide

## Current Status

### ✅ Completed Components

1. **Data Layer (100%)**
   - All entity models (Product, Client, Invoice, Statement, etc.)
   - Complete DbContext with relationships and configurations
   - Database seeding for default settings

2. **Service Layer (100%)**
   - ProductService - full CRUD operations
   - ClientService - full CRUD with invoice validation
   - InvoiceService - full functionality including auto-numbering and duplication
   - StatementService - statement generation and management
   - SettingsService - application configuration
   - PdfService - professional PDF generation using QuestPDF
   - EmailService - SMTP email with attachments and logging

3. **ViewModels (100%)**
   - Base infrastructure (ViewModelBase, RelayCommand, AsyncRelayCommand)
   - MainWindowViewModel - navigation and view management
   - DashboardViewModel - summary statistics
   - InvoiceListViewModel - invoice management
   - ClientListViewModel - client management
   - ProductListViewModel - product management
   - StatementListViewModel - statement management
   - SettingsViewModel - application settings

4. **Main Views (100%)**
   - App.xaml and App.xaml.cs with DI setup
   - MainWindow.xaml - navigation shell
   - DashboardView.xaml - dashboard with statistics
   - InvoiceListView.xaml - invoice list
   - ClientListView.xaml - client list
   - ProductListView.xaml - product list
   - StatementListView.xaml - statement list
   - SettingsView.xaml - comprehensive settings form

### ⚠️ To Be Implemented

1. **Edit Dialog Windows (Priority 1)**
   These are referenced in ViewModels but need XAML implementation:

   - **ClientEditDialog.xaml** - Form for adding/editing clients
   - **ProductEditDialog.xaml** - Form for adding/editing products
   - **InvoiceEditWindow.xaml** - Complex form for invoice creation/editing
   - **StatementGenerateDialog.xaml** - Form for generating statements
   - **StatementViewDialog.xaml** - View statement details

2. **Email Dialog (Priority 2)**
   - **EmailSendDialog.xaml** - Dialog for composing and sending emails with PDF attachments

3. **Additional Features (Optional Enhancements)**
   - Invoice preview before sending
   - Payment recording functionality
   - Advanced search and filtering
   - Data export (CSV, Excel)
   - Backup and restore
   - Reports and analytics

## Implementation Steps

### Step 1: Build and Test Core Application

1. Open `InvoiceApp.sln` in Visual Studio 2022
2. Build the solution (Ctrl+Shift+B)
3. Run the application (F5)
4. Verify:
   - Database is created in `%LocalAppData%\InvoiceApp\invoice.db`
   - Main window loads with navigation
   - All list views display (empty at first)
   - Settings page loads

### Step 2: Implement ClientEditDialog

Create `Views/ClientEditDialog.xaml`:

```xml
<Window x:Class="InvoiceApp.Views.ClientEditDialog"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        Title="Client Details" Height="500" Width="500"
        WindowStartupLocation="CenterScreen">
    <Grid Margin="20">
        <Grid.RowDefinitions>
            <RowDefinition Height="*"/>
            <RowDefinition Height="Auto"/>
        </Grid.RowDefinitions>

        <ScrollViewer Grid.Row="0">
            <StackPanel>
                <Label Content="Company Name:"/>
                <TextBox x:Name="CompanyNameTextBox"/>

                <Label Content="Contact Person:"/>
                <TextBox x:Name="ContactPersonTextBox"/>

                <Label Content="Email:"/>
                <TextBox x:Name="EmailTextBox"/>

                <Label Content="Phone:"/>
                <TextBox x:Name="PhoneTextBox"/>

                <Label Content="Billing Address:"/>
                <TextBox x:Name="BillingAddressTextBox" AcceptsReturn="True" Height="80"/>

                <Label Content="VAT Number:"/>
                <TextBox x:Name="VatNumberTextBox"/>

                <CheckBox x:Name="IsActiveCheckBox" Content="Active" IsChecked="True" Margin="5"/>
            </StackPanel>
        </ScrollViewer>

        <StackPanel Grid.Row="1" Orientation="Horizontal" HorizontalAlignment="Right" Margin="0,10,0,0">
            <Button Content="Save" Width="80" Click="SaveButton_Click" IsDefault="True"/>
            <Button Content="Cancel" Width="80" Click="CancelButton_Click" IsCancel="True"/>
        </StackPanel>
    </Grid>
</Window>
```

Create `Views/ClientEditDialog.xaml.cs`:

```csharp
using InvoiceApp.Models;
using InvoiceApp.Services;
using System.Windows;

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
            }
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(CompanyNameTextBox.Text))
            {
                MessageBox.Show("Company name is required.", "Validation Error", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (_client == null)
            {
                _client = new Client();
            }

            _client.CompanyName = CompanyNameTextBox.Text;
            _client.ContactPerson = ContactPersonTextBox.Text;
            _client.Email = EmailTextBox.Text;
            _client.Phone = PhoneTextBox.Text;
            _client.BillingAddress = BillingAddressTextBox.Text;
            _client.VatNumber = VatNumberTextBox.Text;
            _client.IsActive = IsActiveCheckBox.IsChecked ?? true;

            if (_clientId.HasValue)
            {
                await _clientService.UpdateClientAsync(_client);
            }
            else
            {
                await _clientService.CreateClientAsync(_client);
            }

            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
```

### Step 3: Implement ProductEditDialog

Follow the same pattern as ClientEditDialog with form fields for:
- Name (required)
- Description
- Unit Price (required, decimal)
- Tax Rate (decimal)
- SKU
- IsActive checkbox

Add validation for:
- Name is required
- Unit Price > 0
- SKU uniqueness (use `_productService.SkuExistsAsync()`)

### Step 4: Implement InvoiceEditWindow

This is the most complex dialog. Key features:

1. **Layout Structure**:
   - Top section: Client dropdown, dates, invoice number
   - Middle section: DataGrid for line items
   - Bottom section: Notes, payment terms, totals
   - Button panel: Save, Cancel, Generate PDF, Send Email

2. **Line Items Management**:
   - Use ObservableCollection for line items
   - Add/Remove buttons
   - Product dropdown or manual entry
   - Quantity, price, tax rate inputs
   - Auto-calculate line totals

3. **Total Calculations**:
   - Update totals whenever line items change
   - Display subtotal, tax, and total

4. **Integration**:
   - Load products and clients on initialization
   - Save invoice through InvoiceService
   - Generate PDF through PdfService
   - Send email through EmailService

### Step 5: Implement Statement Dialogs

**StatementGenerateDialog**:
- Client selection
- Date range pickers
- Opening balance input
- Generate button
- Preview generated statement
- Save and export options

**StatementViewDialog**:
- Read-only display of statement
- Client info header
- Transaction DataGrid
- Balance summary
- Export PDF and Send Email buttons

### Step 6: Testing Workflow

1. **Add Test Data**:
   - Add 3-5 clients
   - Add 5-10 products/services
   - Create several invoices with different statuses

2. **Test Core Functions**:
   - Create, edit, delete for all entities
   - Invoice duplication
   - Statement generation
   - PDF export (check output quality)
   - Email sending (configure SMTP first)

3. **Test Edge Cases**:
   - Empty lists
   - Validation errors
   - Delete clients with invoices
   - Invalid email settings
   - Large invoice line items

### Step 7: Polish and Enhancement

1. **UI Improvements**:
   - Add loading indicators for async operations
   - Implement search/filter functionality
   - Add sorting to DataGrids
   - Improve error messages
   - Add confirmation dialogs

2. **Business Logic**:
   - Auto-update overdue invoice status
   - Calculate days overdue
   - Payment recording
   - Partial payments

3. **Reports**:
   - Sales report by period
   - Client aging report
   - Tax summary report

## Key Implementation Patterns

### Dialog Pattern
All edit dialogs should follow this pattern:
1. Constructor accepts service(s) and optional entity ID
2. Load entity if ID provided (edit mode)
3. Populate form fields
4. Validate on save
5. Call service to create/update
6. Set DialogResult = true on success
7. Handle errors with MessageBox

### Async Operations
Always use async/await for:
- Database operations
- File operations (PDF generation)
- Network operations (email)
- Long-running calculations

### Error Handling
```csharp
try
{
    await _service.OperationAsync();
    MessageBox.Show("Success!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
}
catch (Exception ex)
{
    MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
}
```

### ObservableCollection Usage
For dynamic lists that update the UI:
```csharp
private ObservableCollection<InvoiceLineItem> _lineItems = new();
public ObservableCollection<InvoiceLineItem> LineItems
{
    get => _lineItems;
    set => SetProperty(ref _lineItems, value);
}
```

## Database Migrations

If you need to modify the database schema:

1. Add/modify entity in Models folder
2. Update DbContext if needed
3. Open Package Manager Console
4. Run: `Add-Migration MigrationName`
5. Run: `Update-Database`

Or for SQLite with EnsureCreated:
- Delete the database file
- Restart the application (will recreate)

## Deployment

### For Distribution:
1. Build in Release mode
2. Publish the application:
   - Right-click project → Publish
   - Choose "Folder" target
   - Select output location
   - Click Publish

3. Distribute:
   - Include the entire publish folder
   - Requires .NET 8 Runtime on target machine
   - Database will be created on first run

### Installer (Optional):
- Use WiX Toolset or Advanced Installer
- Include .NET 8 Runtime in installer
- Set up Start Menu shortcuts
- Configure file associations

## Common Issues and Solutions

### Issue: Database not found
**Solution**: Check that %LocalAppData%\InvoiceApp folder has write permissions

### Issue: Email fails
**Solution**: 
- Verify SMTP settings
- Check firewall
- For Gmail: Use App Password if 2FA enabled
- Test with "Test Email Connection" button

### Issue: PDF generation fails
**Solution**: Ensure QuestPDF NuGet package is installed and license is set

### Issue: Views not displaying
**Solution**: 
- Check namespace in XAML matches code-behind
- Verify DataTemplate types in MainWindow.xaml
- Check ViewModel namespaces

## Next Steps

1. Implement the 5 edit dialogs (highest priority)
2. Test all CRUD operations
3. Configure email settings and test
4. Generate sample invoices and statements
5. Review PDF output quality
6. Add any custom business logic needed
7. Polish UI based on user feedback
8. Add additional features as needed

## Resources

- **WPF Documentation**: https://docs.microsoft.com/en-us/dotnet/desktop/wpf/
- **Entity Framework Core**: https://docs.microsoft.com/en-us/ef/core/
- **QuestPDF**: https://www.questpdf.com/documentation/
- **MVVM Pattern**: https://docs.microsoft.com/en-us/xamarin/xamarin-forms/enterprise-application-patterns/mvvm

## Support

For implementation questions:
1. Check this guide
2. Review existing code patterns (DashboardView, Services)
3. Check Visual Studio IntelliSense
4. Debug step-by-step with breakpoints
