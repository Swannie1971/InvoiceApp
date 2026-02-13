using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using System;
using Microsoft.EntityFrameworkCore;
using InvoiceApp.Models;

namespace InvoiceApp.Data
{
    public class InvoiceDbContext : DbContext
    {
        public InvoiceDbContext(DbContextOptions<InvoiceDbContext> options)
            : base(options)
        {
        }

        public DbSet<Product> Products { get; set; }
        public DbSet<Client> Clients { get; set; }
        public DbSet<Invoice> Invoices { get; set; }
        public DbSet<InvoiceLineItem> InvoiceLineItems { get; set; }
        public DbSet<Statement> Statements { get; set; }
        public DbSet<StatementLineItem> StatementLineItems { get; set; }
        public DbSet<AppSettings> AppSettings { get; set; }
        public DbSet<EmailLog> EmailLogs { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Payment> Payments { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Product configuration
            modelBuilder.Entity<Product>(entity =>
            {
                entity.Property(e => e.UnitPrice).HasColumnType("decimal(18,2)");
                entity.Property(e => e.TaxRate).HasColumnType("decimal(5,2)");
            });

            // Client configuration
            modelBuilder.Entity<Client>(entity =>
            {
                entity.HasIndex(e => e.Email);
                entity.HasIndex(e => e.CompanyName);
            });

            // Invoice configuration
            modelBuilder.Entity<Invoice>(entity =>
            {
                entity.HasIndex(e => e.InvoiceNumber).IsUnique();
                entity.HasIndex(e => e.ClientId);
                entity.HasIndex(e => e.InvoiceDate);
                entity.HasIndex(e => e.Status);

                entity.Property(e => e.Subtotal).HasColumnType("decimal(18,2)");
                entity.Property(e => e.TaxAmount).HasColumnType("decimal(18,2)");
                entity.Property(e => e.Total).HasColumnType("decimal(18,2)");

                entity.HasOne(e => e.Client)
                    .WithMany(c => c.Invoices)
                    .HasForeignKey(e => e.ClientId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // InvoiceLineItem configuration
            modelBuilder.Entity<InvoiceLineItem>(entity =>
            {
                entity.Property(e => e.Quantity).HasColumnType("decimal(18,2)");
                entity.Property(e => e.UnitPrice).HasColumnType("decimal(18,2)");
                entity.Property(e => e.TaxRate).HasColumnType("decimal(5,2)");
                entity.Property(e => e.LineTotal).HasColumnType("decimal(18,2)");

                entity.HasOne(e => e.Invoice)
                    .WithMany(i => i.LineItems)
                    .HasForeignKey(e => e.InvoiceId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Product)
                    .WithMany()
                    .HasForeignKey(e => e.ProductId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // Statement configuration
            modelBuilder.Entity<Statement>(entity =>
            {
                entity.Property(e => e.OpeningBalance).HasColumnType("decimal(18,2)");
                entity.Property(e => e.ClosingBalance).HasColumnType("decimal(18,2)");

                entity.HasOne(e => e.Client)
                    .WithMany()
                    .HasForeignKey(e => e.ClientId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // StatementLineItem configuration
            modelBuilder.Entity<StatementLineItem>(entity =>
            {
                entity.Property(e => e.Debit).HasColumnType("decimal(18,2)");
                entity.Property(e => e.Credit).HasColumnType("decimal(18,2)");
                entity.Property(e => e.Balance).HasColumnType("decimal(18,2)");

                entity.HasOne(e => e.Statement)
                    .WithMany(s => s.LineItems)
                    .HasForeignKey(e => e.StatementId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Invoice)
                    .WithMany()
                    .HasForeignKey(e => e.InvoiceId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // AppSettings configuration
            modelBuilder.Entity<AppSettings>(entity =>
            {
                entity.Property(e => e.DefaultTaxRate).HasColumnType("decimal(5,2)");
            });

            // EmailLog configuration
            modelBuilder.Entity<EmailLog>(entity =>
            {
                entity.HasIndex(e => e.SentAt);
            });

            // Seed default admin user
            modelBuilder.Entity<User>().HasData(new User
            {
                Id = 1,
                Username = "admin",
                FullName = "Administrator",
                Email = "admin@invoiceapp.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123"), // Default password
                Role = UserRole.Admin,
                IsActive = true,
                CreatedAt = DateTime.Now
            });

            // Seed default settings
            modelBuilder.Entity<AppSettings>().HasData(new AppSettings
            {
                Id = 1,
                CompanyName = "Your Company Name",
                CompanyAddress = "123 Business Street\nCity, State 12345",
                CompanyPhone = "(555) 123-4567",
                CompanyEmail = "info@yourcompany.com",
                InvoicePrefix = "INV",
                InvoiceNextNumber = 1001,
                DefaultTaxRate = 0,
                DefaultPaymentTerms = "Payment due within 30 days",
                InvoiceFooter = "Thank you for your business!",
                SmtpPort = 587,
                SmtpUseSsl = true,
                DefaultEmailSubject = "Invoice #{InvoiceNumber} from {CompanyName}",
                DefaultEmailBody = "Dear {ClientName},\n\nPlease find attached invoice #{InvoiceNumber}.\n\nThank you for your business.\n\nBest regards,\n{CompanyName}",
                CurrencySymbol = "R",
                CurrencyCode = "ZAR"
            });
        }
    }
}


