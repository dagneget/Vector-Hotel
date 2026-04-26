using System;
using System.Threading.Tasks;
using HRS.Models;
using System.Net.Http;

namespace HRS.Services
{
    public static class SettingsService
    {
        public static async Task<SystemSettingsModel> GetSettingsAsync()
        {
            return await ApiService.GetAsync<SystemSettingsModel>("settings");
        }

        public static async Task SaveSettingsAsync(SystemSettingsModel settings)
        {
            await ApiService.PostAsync<object>("settings", settings);
        }

        public static async Task<byte[]> BackupAsync()
        {
            // Note: Since ApiService wrapper is simple, we might need direct access for File download
            // or return a byte array. For now, assume GetAsync or similar can handle it if we adapt.
            // Let's use a simpler approach for the backup download in this environment.
            using (var client = new HttpClient())
            {
                var response = await client.PostAsync("http://localhost:5262/api/settings/backup", null);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadAsByteArrayAsync();
            }
        }

        public static async Task<bool> RestoreAsync(string jsonContent)
        {
            try
            {
                await ApiService.PostAsync<object>("settings/restore", jsonContent);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
