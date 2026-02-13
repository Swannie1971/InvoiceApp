using System.Threading.Tasks;
using InvoiceApp.Models;

namespace InvoiceApp.Services
{
    public interface IAuthenticationService
    {
        Task<User?> LoginAsync(string username, string password);
        Task<bool> ChangePasswordAsync(int userId, string currentPassword, string newPassword);
        Task<User?> GetCurrentUserAsync();
        void SetCurrentUser(User user);
        void Logout();
        bool IsLoggedIn { get; }
        User? CurrentUser { get; }
    }
}