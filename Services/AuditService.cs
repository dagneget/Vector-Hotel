using System;
using HRS.Models;

namespace HRS.Services
{
    public static class AuditService
    {
        public static async void Log(string action, string details)
        {
            var user = AuthService.CurrentUser;
            var log = new AuditLogModel
            {
                Id = DataStore.GenerateId(),
                UserId = user?.Id ?? "System",
                UserRole = user?.Role ?? "System",
                Action = action,
                Details = details,
                Timestamp = DateTime.Now
            };
            
            try 
            {
                await ApiService.PostAsync<AuditLogModel>("auditlogs", log);
                DataStore.Data.AuditLogs.Insert(0, log);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Audit Log Fail: {ex.Message}");
            }
        }
    }
}
