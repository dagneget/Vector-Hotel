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
            try 
            {
                var response = await ApiService.PostAsync<UserModel>("users/login", new { Username = username, Password = password });
                if (response != null)
                {
                    CurrentUser = response;
                    await DataStore.LoadAsync(); // Load all data once logged in
                    return true;
                }
            }
            catch (Exception)
            {
                // Login failed or API unreachable
            }
            return false;
        }

        public static void Logout()
        {
            CurrentUser = null;
        }

        /// <summary>Returns true if the current user is an Admin.</summary>
        public static bool IsAdmin() => CurrentUser?.Role == "Admin";
        
        public static bool IsReceptionist() => CurrentUser?.Role == "Receptionist";
        
        public static bool IsAccountant() => CurrentUser?.Role == "Accountant";

        /// <summary>Legacy alias for IsAdmin(). Kept for backward compatibility.</summary>
        public static bool CanDeleteRoom() => IsAdmin();
    }
}
