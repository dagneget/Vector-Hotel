using System.Collections.ObjectModel;
using System.Linq;
using HRS.Models;
using HRS.Services;

namespace HRS.ViewModels
{
    public class AuditLogsViewModel : ViewModelBase
    {
        private ObservableCollection<AuditLogModel> _auditLogsList;
        public ObservableCollection<AuditLogModel> AuditLogsList
        {
            get => _auditLogsList;
            set => SetProperty(ref _auditLogsList, value);
        }

        // --- Stats ---
        public int TotalLogins => DataStore.Data.AuditLogs.Count(l => l.Action?.Contains("Login") == true);
        public int SecurityAlerts => DataStore.Data.AuditLogs.Count(l => l.Severity == "Critical");
        public int DataModifications => DataStore.Data.AuditLogs.Count(l => l.Severity == "Warning" || (l.Action?.Contains("Update") == true));

        // --- Filtering ---
        private string _selectedCategory = "All";
        public string SelectedCategory
        {
            get => _selectedCategory;
            set { if (SetProperty(ref _selectedCategory, value)) FilterLogs(); }
        }

        private string _selectedExecutor = "All";
        public string SelectedExecutor
        {
            get => _selectedExecutor;
            set { if (SetProperty(ref _selectedExecutor, value)) FilterLogs(); }
        }

        private string _selectedSeverity = "All";
        public string SelectedSeverity
        {
            get => _selectedSeverity;
            set { if (SetProperty(ref _selectedSeverity, value)) FilterLogs(); }
        }

        private string _searchText = "";
        public string SearchText
        {
            get => _searchText;
            set { if (SetProperty(ref _searchText, value)) FilterLogs(); }
        }

        public string[] CategoryOptions => new[] { "All", "Access", "Financial", "Modification", "System" };
        public string[] SeverityOptions => new[] { "All", "Critical", "Warning", "Info" };
        
        public ObservableCollection<string> ExecutorOptions { get; } = new ObservableCollection<string>();

        public System.Windows.Input.ICommand GenerateReportCommand { get; }

        public AuditLogsViewModel()
        {
            GenerateReportCommand = new RelayCommand(_ => GenerateReport());
            LoadData();
            AuditService.Log("Security Module Accessed", "Admin opened the Security Audit Stream.", "Access", "Info");
        }

        private void LoadData()
        {
            if (DataStore.Data.AuditLogs.Count == 0)
            {
                AuditService.Log("System Online", "Security audit engine initialized successfully.", "System", "Info");
                AuditService.Log("Database Sync", "Connected to security log repository.", "System", "Info");
            }

            // Populate Executor Options
            var executors = DataStore.Data.AuditLogs
                .Select(l => l.UserRole)
                .Where(r => !string.IsNullOrEmpty(r))
                .Distinct()
                .OrderBy(r => r)
                .ToList();

            ExecutorOptions.Clear();
            ExecutorOptions.Add("All");
            foreach (var ex in executors) ExecutorOptions.Add(ex);

            FilterLogs();
        }

        private void FilterLogs()
        {
            var query = DataStore.Data.AuditLogs.AsEnumerable();
            
            if (SelectedCategory != "All")
                query = query.Where(l => l.Category == SelectedCategory);

            if (SelectedExecutor != "All")
                query = query.Where(l => l.UserRole == SelectedExecutor);

            if (SelectedSeverity != "All")
                query = query.Where(l => l.Severity == SelectedSeverity);

            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                string lowerSearch = SearchText.ToLower();
                query = query.Where(l => 
                    (l.Action?.ToLower().Contains(lowerSearch) == true) || 
                    (l.Details?.ToLower().Contains(lowerSearch) == true) || 
                    (l.UserRole?.ToLower().Contains(lowerSearch) == true));
            }

            AuditLogsList = new ObservableCollection<AuditLogModel>(query.OrderByDescending(log => log.Timestamp));
            
            // Refresh stats
            OnPropertyChanged(nameof(TotalLogins));
            OnPropertyChanged(nameof(SecurityAlerts));
            OnPropertyChanged(nameof(DataModifications));
        }

        private void GenerateReport()
        {
            // Simulate report generation
            string docPath = System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments), "SecurityAuditReport.txt");
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("SECURITY AUDIT REPORT - NOCTURNAL");
            sb.AppendLine($"Generated: {System.DateTime.Now}");
            sb.AppendLine("--------------------------------");
            foreach (var log in AuditLogsList)
            {
                sb.AppendLine($"{log.Timestamp} | {log.Severity,-8} | {log.UserRole,-12} | {log.Action}");
            }
            System.IO.File.WriteAllText(docPath, sb.ToString());
            System.Windows.MessageBox.Show($"Security Audit Report generated at: {docPath}", "Report Complete");
        }
    }
}
