using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HRS.Models;

namespace HRS.Services
{
    public static class UserService
    {
        public static async Task<List<UserModel>> GetUsersAsync()
        {
            return await ApiService.GetAsync<List<UserModel>>("users");
        }

        public static async Task<UserModel> CreateUserAsync(UserModel user)
        {
            user.Id = DataStore.GenerateId();
            return await ApiService.PostAsync<UserModel>("users", user);
        }

        public static async Task UpdateUserAsync(UserModel user)
        {
            await ApiService.PutAsync($"users/{user.Id}", user);
        }

        public static async Task DeleteUserAsync(string id)
        {
            await ApiService.DeleteAsync($"users/{id}");
        }

        public static async Task<bool> ChangePasswordAsync(string userId, string currentPassword, string newPassword)
        {
            try
            {
                await ApiService.PostAsync<object>($"users/{userId}/change-password", new 
                { 
                    CurrentPassword = currentPassword, 
                    NewPassword = newPassword 
                });
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static async Task<bool> ResetPasswordAsync(string userId, string newPassword)
        {
            try
            {
                await ApiService.PostAsync<object>($"users/{userId}/reset-password", newPassword);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
