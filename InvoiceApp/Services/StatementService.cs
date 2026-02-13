using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using System;
using InvoiceApp.Data;
using InvoiceApp.Models;
using Microsoft.EntityFrameworkCore;

namespace InvoiceApp.Services
{
    public interface IStatementService
    {
        Task<List<Statement>> GetAllStatementsAsync();
        Task<Statement?> GetStatementByIdAsync(int id);
        Task<Statement> CreateStatementAsync(Statement statement);
        Task<Statement> UpdateStatementAsync(Statement statement);
        Task DeleteStatementAsync(int id);
        Task<Statement> GenerateStatementAsync(int clientId, DateTime startDate, DateTime endDate, decimal openingBalance);
    }

    public class StatementService : IStatementService
    {
        private readonly InvoiceDbContext _context;

        public StatementService(InvoiceDbContext context)
        {
            _context = context;
        }

        public async Task<List<Statement>> GetAllStatementsAsync()
        {
            return await _context.Statements
                .Include(s => s.Client)
                .Include(s => s.LineItems)
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();
        }

        public async Task<Statement?> GetStatementByIdAsync(int id)
        {
            return await _context.Statements
                .Include(s => s.Client)
                .Include(s => s.LineItems)
                    .ThenInclude(li => li.Invoice)
                .FirstOrDefaultAsync(s => s.Id == id);
        }

        public async Task<Statement> CreateStatementAsync(Statement statement)
        {
            CalculateStatementBalances(statement);
            statement.CreatedAt = DateTime.Now;

            _context.Statements.Add(statement);
            await _context.SaveChangesAsync();

            return statement;
        }

        public async Task<Statement> UpdateStatementAsync(Statement statement)
        {
            CalculateStatementBalances(statement);

            _context.Statements.Update(statement);
            await _context.SaveChangesAsync();

            return statement;
        }

        public async Task DeleteStatementAsync(int id)
        {
            var statement = await _context.Statements
                .Include(s => s.LineItems)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (statement != null)
            {
                _context.Statements.Remove(statement);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<Statement> GenerateStatementAsync(int clientId, DateTime startDate, DateTime endDate, decimal openingBalance)
        {
            var client = await _context.Clients.FindAsync(clientId);
            if (client == null)
                throw new ArgumentException("Client not found");

            var statement = new Statement
            {
                ClientId = clientId,
                Client = client,
                StatementDate = DateTime.Now,
                StartDate = startDate,
                EndDate = endDate,
                OpeningBalance = openingBalance
            };

            // Get invoices in date range
            var invoices = await _context.Invoices
                .Include(i => i.Payments)
                .Where(i => i.ClientId == clientId && i.InvoiceDate >= startDate && i.InvoiceDate <= endDate)
                .OrderBy(i => i.InvoiceDate)
                .ToListAsync();

            // Create a list of all transactions (invoices and payments)
            var transactions = new List<(DateTime Date, string Description, decimal Debit, decimal Credit)>();

            // Add invoices as debits
            foreach (var invoice in invoices)
            {
                transactions.Add((
                    invoice.InvoiceDate,
                    $"Invoice {invoice.InvoiceNumber}",
                    invoice.Total,
                    0
                ));

                // Add payments for this invoice as credits
                foreach (var payment in invoice.Payments.Where(p => p.PaymentDate >= startDate && p.PaymentDate <= endDate))
                {
                    transactions.Add((
                        payment.PaymentDate,
                        $"Payment - Invoice {invoice.InvoiceNumber} ({payment.PaymentMethod})",
                        0,
                        payment.Amount
                    ));
                }
            }

            // Sort all transactions by date
            transactions = transactions.OrderBy(t => t.Date).ToList();

            // Create statement line items with running balance
            decimal runningBalance = openingBalance;
            int sortOrder = 0;

            foreach (var transaction in transactions)
            {
                runningBalance = runningBalance + transaction.Debit - transaction.Credit;

                var lineItem = new StatementLineItem
                {
                    Date = transaction.Date,
                    Description = transaction.Description,
                    Debit = transaction.Debit,
                    Credit = transaction.Credit,
                    Balance = runningBalance,
                    SortOrder = sortOrder++
                };

                statement.LineItems.Add(lineItem);
            }

            statement.ClosingBalance = runningBalance;

            _context.Statements.Add(statement);
            await _context.SaveChangesAsync();

            return statement;
        }

        private void CalculateStatementBalances(Statement statement)
        {
            decimal balance = statement.OpeningBalance;

            foreach (var item in statement.LineItems.OrderBy(li => li.SortOrder))
            {
                balance += item.Debit - item.Credit;
                item.Balance = balance;
            }

            statement.ClosingBalance = balance;
        }
    }
}


