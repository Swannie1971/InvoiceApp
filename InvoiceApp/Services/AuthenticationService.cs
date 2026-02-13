using System;
using System.Linq;
using System.Threading.Tasks;
using InvoiceApp.Data;
using InvoiceApp.Models;
using Microsoft.EntityFrameworkCore;

namespace InvoiceApp.Services
{
    public class AuthenticationService : IAuthenticationService
    {
        private readonly InvoiceDbContext _context;
        private User? _currentUser;

        public AuthenticationService(InvoiceDbContext context)
        {
            _context = context;
        }

        public bool IsLoggedIn => _currentUser != null;

        public User? CurrentUser => _currentUser;

        public async Task<User?> LoginAsync(string username, string password)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == username && u.IsActive);

            if (user == null)
                return null;

            // Verify password
            if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
                return null;

            // Update last login
            user.LastLoginAt = DateTime.Now;
            await _context.SaveChangesAsync();

            _currentUser = user;
            return user;
        }

        public async Task<bool> ChangePasswordAsync(int userId, string currentPassword, string newPassword)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return false;

            // Verify current password
            if (!BCrypt.Net.BCrypt.Verify(currentPassword, user.PasswordHash))
                return false;

            // Set new password
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
            await _context.SaveChangesAsync();

            return true;
        }

        public Task<User?> GetCurrentUserAsync()
        {
            return Task.FromResult(_currentUser);
        }

        public void SetCurrentUser(User user)
        {
            _currentUser = user;
        }

        public void Logout()
        {
            _currentUser = null;
        }
    }
}