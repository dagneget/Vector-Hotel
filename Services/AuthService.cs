using System;
using System.Linq;
using System.Threading.Tasks;
using HRS.Models;

namespace HRS.Services
{
    public static class AuthService
    {
        public static UserModel CurrentUser { get; private set; }

        public static async Task<bool> LoginAsync(string username, string password)
        {
            var response = await ApiService.PostAsync<UserModel>("users/login", new { Username = username, Password = password });
            if (response != null)
            {
                CurrentUser = response;
                await DataStore.LoadAsync(); // Load all data once logged in
                return true;
            }
            return false;
        }

        public static void Logout()
        {
            CurrentUser = null;
        }

        /// <summary>Returns true if the current user is an Admin.</summary>
        public static bool IsAdmin() => CurrentUser?.Role?.Equals("Admin", StringComparison.OrdinalIgnoreCase) == true;
        
        public static bool IsReceptionist() => CurrentUser?.Role?.Equals("Receptionist", StringComparison.OrdinalIgnoreCase) == true || 
                                              CurrentUser?.Role?.Equals("Reception", StringComparison.OrdinalIgnoreCase) == true;
        
        public static bool IsAccountant() => CurrentUser?.Role?.Equals("Accountant", StringComparison.OrdinalIgnoreCase) == true || 
                                            CurrentUser?.Role?.Equals("Finance", StringComparison.OrdinalIgnoreCase) == true;

        /// <summary>Legacy alias for IsAdmin(). Kept for backward compatibility.</summary>
        public static bool CanDeleteRoom() => IsAdmin();
    }
}
