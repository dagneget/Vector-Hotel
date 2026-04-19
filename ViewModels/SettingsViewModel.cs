using System.Collections.ObjectModel;
using System.Linq;
using HRS.Models;
using HRS.Services;

namespace HRS.ViewModels
{
    public class SettingsViewModel : ViewModelBase
    {
        public ObservableCollection<UserModel> UsersList { get; set; }
        public ObservableCollection<AuditLogModel> AuditLogsList { get; set; }

        public SettingsViewModel()
        {
            UsersList = new ObservableCollection<UserModel>(DataStore.Data.Users);
            AuditLogsList = new ObservableCollection<AuditLogModel>(DataStore.Data.AuditLogs.OrderByDescending(log => log.Timestamp));
            
            AuditService.Log("Accessed Settings", "Admin accessed system settings and audit logs.");
        }
    }
}
