using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using System;
using InvoiceApp.Data;
using InvoiceApp.Models;
using Microsoft.EntityFrameworkCore;

namespace InvoiceApp.Services
{
    public interface IClientService
    {
        Task<List<Client>> GetAllClientsAsync();
        Task<List<Client>> GetActiveClientsAsync();
        Task<Client?> GetClientByIdAsync(int id);
        Task<Client> CreateClientAsync(Client client);
        Task<Client> UpdateClientAsync(Client client);
        Task DeleteClientAsync(int id);
        Task<bool> HasInvoicesAsync(int clientId);
    }

    public class ClientService : IClientService
    {
        private readonly InvoiceDbContext _context;

        public ClientService(InvoiceDbContext context)
        {
            _context = context;
        }

        public async Task<List<Client>> GetAllClientsAsync()
        {
            return await _context.Clients
                .OrderBy(c => c.CompanyName)
                .ToListAsync();
        }

        public async Task<List<Client>> GetActiveClientsAsync()
        {
            return await _context.Clients
                .Where(c => c.IsActive)
                .OrderBy(c => c.CompanyName)
                .ToListAsync();
        }

        public async Task<Client?> GetClientByIdAsync(int id)
        {
            return await _context.Clients
                .Include(c => c.Invoices)
                .FirstOrDefaultAsync(c => c.Id == id);
        }

        public async Task<Client> CreateClientAsync(Client client)
        {
            client.CreatedAt = DateTime.Now;
            _context.Clients.Add(client);
            await _context.SaveChangesAsync();
            return client;
        }

        public async Task<Client> UpdateClientAsync(Client client)
        {
            client.UpdatedAt = DateTime.Now;
            _context.Clients.Update(client);
            await _context.SaveChangesAsync();
            return client;
        }

        public async Task DeleteClientAsync(int id)
        {
            var client = await _context.Clients.FindAsync(id);
            if (client != null)
            {
                // Soft delete - just mark as inactive
                client.IsActive = false;
                client.UpdatedAt = DateTime.Now;
                await _context.SaveChangesAsync();
            }
        }

        public async Task<bool> HasInvoicesAsync(int clientId)
        {
            return await _context.Invoices
                .AnyAsync(i => i.ClientId == clientId);
        }
    }
}


