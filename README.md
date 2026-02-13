# Invoice App - Windows Desktop Invoicing Application

## Overview
A comprehensive Windows desktop application for small business invoicing and client management built with WPF, C#, .NET 8, Entity Framework Core, and SQLite.

## Technology Stack
- **Language**: C# 10
- **Framework**: .NET 8 (Windows)
- **UI**: WPF (Windows Presentation Foundation)
- **Database**: SQLite (local file-based)
- **ORM**: Entity Framework Core 8.0
- **PDF Generation**: QuestPDF 2024.12.0
- **Email**: System.Net.Mail (built-in SMTP)
- **Architecture**: MVVM (Model-View-ViewModel)
- **Dependency Injection**: Microsoft.Extensions.DependencyInjection

## Features

### Core Features
1. **Products & Services Management**
   - Add, edit, delete products/services
   - Track name, description, unit price, tax rate, SKU
   - Soft delete (mark as inactive)

2. **Client Management**
   - Complete client database
   - Company info, contacts, billing address, VAT numbers
   - Track client invoices and statements
   - Soft delete with invoice validation

3. **Invoice Management**
   - Create, edit, delete invoices
   - Auto-generate invoice numbers (configurable prefix + sequence)
   - Line items with quantity, pricing, tax
   - Invoice statuses: Draft, Sent, Paid, Overdue
   - Duplicate existing invoices
   - Calculated subtotals, tax, totals

4. **Statement Generation**
   - Generate client statements for date ranges
   - Opening/closing balances
   - Invoice and payment tracking
   - Save and edit statements

5. **PDF Export**
   - Professional invoice PDFs
   - Statement PDFs
   - Company logo and branding
   - Itemized tables with tax summary
   - Customizable footer

6. **Email Functionality**
   - Send invoices and statements via SMTP
   - Configurable email templates
   - PDF attachment
   - Email tracking and logging
   - Test email connection

7. **Settings & Configuration**
   - Company profile (name, address, logo, contact info)
   - Invoice numbering rules
   - Default tax rates
   - Email SMTP configuration
   - Customizable templates

## Project Structure

```
InvoiceApp/
├── Models/                     # Entity models
│   ├── Product.cs
│   ├── Client.cs
│   ├── Invoice.cs
│   ├── InvoiceLineItem.cs
│   ├── Statement.cs
│   ├── AppSettings.cs
│   └── EmailLog.cs
├── Data/                       # Database context
│   └── InvoiceDbContext.cs
├── Services/                   # Business logic
│   ├── ProductService.cs
│   ├── ClientService.cs
│   ├── InvoiceService.cs
│   ├── StatementService.cs
│   ├── SettingsService.cs
│   ├── PdfService.cs
│   └── EmailService.cs
├── ViewModels/                 # MVVM ViewModels
│   ├── ViewModelBase.cs
│   ├── RelayCommand.cs
│   ├── MainWindowViewModel.cs
│   ├── DashboardViewModel.cs
│   ├── InvoiceListViewModel.cs
│   ├── ClientListViewModel.cs
│   ├── ProductListViewModel.cs
│   ├── StatementListViewModel.cs
│   └── SettingsViewModel.cs
├── Views/                      # WPF Views
│   ├── MainWindow.xaml
│   ├── DashboardView.xaml
│   ├── InvoiceListView.xaml (to be implemented)
│   ├── ClientListView.xaml (to be implemented)
│   ├── ProductListView.xaml (to be implemented)
│   ├── StatementListView.xaml (to be implemented)
│   ├── SettingsView.xaml (to be implemented)
│   └── [Dialog windows for edit operations]
├── App.xaml                    # Application entry point
├── App.xaml.cs                 # DI configuration
└── InvoiceApp.csproj          # Project file
```

## Database Schema

### Products
- Id, Name, Description, UnitPrice, TaxRate, Sku, IsActive, CreatedAt, UpdatedAt

### Clients
- Id, CompanyName, ContactPerson, Email, Phone, BillingAddress, VatNumber, IsActive, CreatedAt, UpdatedAt

### Invoices
- Id, InvoiceNumber, ClientId, InvoiceDate, DueDate, Notes, PaymentTerms
- Subtotal, TaxAmount, Total, Status, SentDate, PaidDate, CreatedAt, UpdatedAt

### InvoiceLineItems
- Id, InvoiceId, ProductId, Description, Quantity, UnitPrice, TaxRate, LineTotal, SortOrder

### Statements
- Id, ClientId, StartDate, EndDate, OpeningBalance, ClosingBalance, CreatedAt, Notes

### StatementLineItems
- Id, StatementId, InvoiceId, Date, Description, Debit, Credit, Balance, SortOrder

### AppSettings
- Company profile settings
- Invoice configuration
- Email SMTP settings
- Templates

### EmailLogs
- Id, InvoiceId, StatementId, RecipientEmail, Subject, Body, Success, ErrorMessage, SentAt

## Build Instructions

### Prerequisites
- Windows 10 or Windows 11
- Visual Studio 2022 (Community, Professional, or Enterprise)
- .NET 8 SDK

### Building the Application

1. **Clone or Extract the Project**
   ```
   Extract InvoiceApp folder to your desired location
   ```

2. **Open in Visual Studio 2022**
   - Launch Visual Studio 2022
   - File → Open → Project/Solution
   - Navigate to `InvoiceApp.sln` and open it

3. **Restore NuGet Packages**
   - Visual Studio will automatically restore packages
   - Or manually: Right-click Solution → Restore NuGet Packages

4. **Build the Solution**
   - Build → Build Solution (Ctrl+Shift+B)
   - Or F6

5. **Run the Application**
   - Debug → Start Debugging (F5)
   - Or Debug → Start Without Debugging (Ctrl+F5)

### First Run
On first launch, the application will:
1. Create the SQLite database in `%LocalAppData%\InvoiceApp\invoice.db`
2. Initialize the database schema
3. Seed default settings

## Configuration

### Database Location
The SQLite database is stored at:
```
C:\Users\[YourUsername]\AppData\Local\InvoiceApp\invoice.db
```

### Initial Setup
1. Launch the application
2. Go to Settings
3. Configure:
   - Company profile (name, address, logo, contact info)
   - Invoice numbering (prefix, starting number)
   - Default tax rate
   - Email SMTP settings (for sending invoices)

## Usage Guide

### Managing Products/Services
1. Navigate to "Products/Services"
2. Click "Add Product" to create new products
3. Edit or delete existing products
4. Products can be selected when creating invoices

### Managing Clients
1. Navigate to "Clients"
2. Add client information
3. Edit or view client details
4. Cannot delete clients with existing invoices (will be marked inactive)

### Creating Invoices
1. Navigate to "Invoices"
2. Click "Create Invoice"
3. Select client
4. Add line items (products/services)
5. Set dates and payment terms
6. Save as Draft or mark as Sent
7. Generate PDF or email to client

### Generating Statements
1. Navigate to "Statements"
2. Click "Generate Statement"
3. Select client and date range
4. Review transactions and balances
5. Save and/or export to PDF

### Sending Emails
1. Configure SMTP settings in Settings
2. Test email connection
3. From invoice or statement, click "Send Email"
4. Review/edit subject and body
5. PDF is automatically attached

## Extending the Application

### Adding New Features
The application is designed for easy extension:

1. **New Entity**: Add to Models folder, update DbContext, create service
2. **New View**: Create XAML view, ViewModel, add to navigation
3. **New Service**: Implement interface, register in App.xaml.cs DI
4. **New Report**: Extend PdfService with new document template

### Implementing Missing Views
Several views need XAML implementation:
- InvoiceListView.xaml
- ClientListView.xaml
- ProductListView.xaml
- StatementListView.xaml
- SettingsView.xaml
- Edit dialogs (ClientEditDialog, ProductEditDialog, InvoiceEditWindow, etc.)

Follow the DashboardView.xaml pattern:
1. Create UserControl
2. Bind to ViewModel properties
3. Use DataGrid for lists
4. Add command buttons

## Design Patterns

### MVVM (Model-View-ViewModel)
- **Models**: Data entities (Models folder)
- **Views**: XAML UI (Views folder)
- **ViewModels**: UI logic, data binding (ViewModels folder)
- **Commands**: RelayCommand, AsyncRelayCommand for user actions
- **Services**: Business logic (Services folder)

### Repository Pattern
Services act as repositories, abstracting data access from ViewModels.

### Dependency Injection
All services are registered in App.xaml.cs and injected via constructor injection.

## Troubleshooting

### Database Issues
- Database location: `%LocalAppData%\InvoiceApp\invoice.db`
- Delete database file to reset (will lose all data)
- Check file permissions

### Email Issues
- Verify SMTP settings
- Check firewall/antivirus
- Use "Test Email Connection" in Settings
- Gmail requires "App Passwords" if 2FA is enabled

### Build Errors
- Ensure .NET 8 SDK is installed
- Restore NuGet packages
- Clean and rebuild solution

## Future Enhancements

Potential features for future versions:
- Multi-currency support
- Recurring invoices
- Payment recording and tracking
- Advanced reporting and analytics
- Data export (CSV, Excel)
- Backup and restore
- Multi-user support
- Cloud synchronization
- Mobile app integration
- Barcode/QR code generation

## License

This application is provided as-is for use by small businesses.

## Support

For issues or questions:
1. Check this README
2. Review code comments
3. Examine error messages and logs

## Version History

### Version 1.0.0 (Initial Release)
- Complete MVVM architecture
- SQLite database with EF Core
- Product, Client, Invoice, Statement management
- PDF generation (QuestPDF)
- Email functionality (SMTP)
- Settings and configuration
- Clean, maintainable code structure
