using System;
using HRS.Models;

namespace HRS.Services
{
    public static class AuditService
    {
        public static async void Log(string action, string details, string category = "System", string severity = "Info")
        {
            var user = AuthService.CurrentUser;
            
            // Auto-detect severity if not provided for common actions
            if (severity == "Info")
            {
                if (action.Contains("Delete") || action.Contains("Refund")) severity = "Critical";
                else if (action.Contains("Update") || action.Contains("Edit")) severity = "Warning";
            }

            var log = new AuditLogModel
            {
                Id = DataStore.GenerateId(),
                UserId = user?.Id ?? "System",
                UserRole = user?.Role ?? "System",
                Action = action,
                Details = details,
                Category = category,
                Severity = severity,
                IpAddress = "192.168.1." + new Random().Next(10, 99), // Simulated IP
                Timestamp = DateTime.Now
            };
            
            try 
            {
                // Run API call in background
                await ApiService.PostAsync<AuditLogModel>("auditlogs", log);
                
                // Update UI collection on UI thread
                System.Windows.Application.Current.Dispatcher.Invoke(() => 
                {
                    DataStore.Data.AuditLogs.Insert(0, log);
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Audit Log Fail: {ex.Message}");
            }
        }
    }
}
