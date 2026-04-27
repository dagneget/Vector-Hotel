using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;
using Newtonsoft.Json;
using HRS.Models;
using HRS.Services;

namespace HRS.ViewModels
{
    public class SettingsViewModel : ViewModelBase
    {
        public ObservableCollection<UserModel> UsersList { get; set; }

        // --- Hotel Identity Settings ---
        private string _hotelName;
        public string HotelName { get => _hotelName; set => SetProperty(ref _hotelName, value); }

        private string _hotelAddress;
        public string HotelAddress { get => _hotelAddress; set => SetProperty(ref _hotelAddress, value); }

        private string _hotelPhone;
        public string HotelPhone { get => _hotelPhone; set => SetProperty(ref _hotelPhone, value); }

        private string _hotelEmail;
        public string HotelEmail { get => _hotelEmail; set => SetProperty(ref _hotelEmail, value); }

        // --- Financial Settings ---
        private string _defaultCurrency;
        public string DefaultCurrency { get => _defaultCurrency; set => SetProperty(ref _defaultCurrency, value); }

        private decimal _taxRate;
        public decimal TaxRate { get => _taxRate; set => SetProperty(ref _taxRate, value); }

        private bool _requireFullPaymentBeforeCheckIn;
        public bool RequireFullPaymentBeforeCheckIn { get => _requireFullPaymentBeforeCheckIn; set => SetProperty(ref _requireFullPaymentBeforeCheckIn, value); }

        private bool _allowPartialPayments;
        public bool AllowPartialPayments { get => _allowPartialPayments; set => SetProperty(ref _allowPartialPayments, value); }

        // --- Export Settings ---
        private bool _exportRooms = true;
        public bool ExportRooms { get => _exportRooms; set => SetProperty(ref _exportRooms, value); }

        private bool _exportGuests = true;
        public bool ExportGuests { get => _exportGuests; set => SetProperty(ref _exportGuests, value); }

        private bool _exportReservations = true;
        public bool ExportReservations { get => _exportReservations; set => SetProperty(ref _exportReservations, value); }

        private DateTime? _exportStartDate = DateTime.Now.AddMonths(-1);
        public DateTime? ExportStartDate { get => _exportStartDate; set => SetProperty(ref _exportStartDate, value); }

        private DateTime? _exportEndDate = DateTime.Now;
        public DateTime? ExportEndDate { get => _exportEndDate; set => SetProperty(ref _exportEndDate, value); }

        private SettingsModel _currentSettings;

        // --- User Form Properties ---
        private UserModel _selectedUser;
        public UserModel SelectedUser
        {
            get => _selectedUser;
            set
            {
                if (SetProperty(ref _selectedUser, value))
                {
                    if (value != null)
                    {
                        FormUsername = value.Username;
                        FormRole = value.Role;
                        FormPassword = ""; // Clear password field for security
                    }
                }
            }
        }

        private string _formUsername;
        public string FormUsername { get => _formUsername; set => SetProperty(ref _formUsername, value); }

        private string _formPassword;
        public string FormPassword { get => _formPassword; set => SetProperty(ref _formPassword, value); }

        private string _formRole;
        public string FormRole { get => _formRole; set => SetProperty(ref _formRole, value); }

        public string[] RoleOptions => new[] { "Admin", "Receptionist", "Finance" };

        // --- Commands ---
        public ICommand SaveSettingsCommand { get; }
        public ICommand BackupCommand { get; }
        public ICommand RestoreCommand { get; }
        public ICommand AddUserCommand { get; }
        public ICommand DeleteUserCommand { get; }
        public ICommand ExportDataCommand { get; }

        public SettingsViewModel()
        {
            UsersList = new ObservableCollection<UserModel>(DataStore.Data.Users);
            
            SaveSettingsCommand = new RelayCommand(_ => SaveSettings());
            BackupCommand = new RelayCommand(_ => BackupDatabase());
            RestoreCommand = new RelayCommand(_ => RestoreDatabase());
            AddUserCommand = new RelayCommand(AddUser);
            DeleteUserCommand = new RelayCommand(_ => DeleteSelectedUser());
            ExportDataCommand = new RelayCommand(_ => ExportData());

            FormRole = RoleOptions.FirstOrDefault();

            LoadSettings();
            AuditService.Log("Accessed Settings", "Admin accessed system settings.");
        }

        private async void LoadSettings()
        {
            try
            {
                _currentSettings = await ApiService.GetAsync<SettingsModel>("settings");
                if (_currentSettings != null)
                {
                    HotelName = _currentSettings.HotelName;
                    HotelAddress = _currentSettings.HotelAddress;
                    HotelPhone = _currentSettings.HotelPhone;
                    HotelEmail = _currentSettings.HotelEmail;
                    DefaultCurrency = _currentSettings.DefaultCurrency;
                    TaxRate = _currentSettings.TaxRate;
                    RequireFullPaymentBeforeCheckIn = _currentSettings.RequireFullPaymentBeforeCheckIn;
                    AllowPartialPayments = _currentSettings.AllowPartialPayments;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading settings: {ex.Message}");
            }
        }

        private async void SaveSettings()
        {
            if (!IsValid)
            {
                var errors = string.Join("\n", AllErrors);
                MessageBox.Show($"Please fix the following errors:\n\n{errors}", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (_currentSettings == null) _currentSettings = new SettingsModel();

            _currentSettings.HotelName = HotelName;
            _currentSettings.HotelAddress = HotelAddress;
            _currentSettings.HotelPhone = HotelPhone;
            _currentSettings.HotelEmail = HotelEmail;
            _currentSettings.DefaultCurrency = DefaultCurrency;
            _currentSettings.TaxRate = TaxRate;
            _currentSettings.RequireFullPaymentBeforeCheckIn = RequireFullPaymentBeforeCheckIn;
            _currentSettings.AllowPartialPayments = AllowPartialPayments;

            try
            {
                await ApiService.PutAsync("settings", _currentSettings);
                MessageBox.Show("Settings saved successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                AuditService.Log("Updated Settings", "Updated core system and hotel preferences.");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving settings: {ex.Message}");
            }
        }

        private async void BackupDatabase()
        {
            try
            {
                var backupData = await ApiService.PostAsync<Newtonsoft.Json.Linq.JToken>("settings/backup", new { });
                
                SaveFileDialog sfd = new SaveFileDialog
                {
                    Filter = "JSON Files (*.json)|*.json",
                    FileName = $"HRS_Backup_{DateTime.Now:yyyyMMdd}.json",
                    Title = "Export System Backup"
                };

                if (sfd.ShowDialog() == true)
                {
                    File.WriteAllText(sfd.FileName, backupData.ToString(Formatting.Indented));
                    MessageBox.Show($"Backup successfully saved to:\n{sfd.FileName}", "Backup Complete", MessageBoxButton.OK, MessageBoxImage.Information);
                    AuditService.Log("Database Backup", $"Exported system state to {Path.GetFileName(sfd.FileName)}");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Backup failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void RestoreDatabase()
        {
            var warningResult = MessageBox.Show(
                "WARNING: Restoring will OVERWRITE all existing data (Reservations, Guests, Rooms).\n\nAre you absolutely sure you want to proceed?",
                "Critical Warning",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (warningResult != MessageBoxResult.Yes) return;

            try
            {
                OpenFileDialog ofd = new OpenFileDialog
                {
                    Filter = "JSON Files (*.json)|*.json",
                    Title = "Import System Backup"
                };

                if (ofd.ShowDialog() == true)
                {
                    string jsonContent = File.ReadAllText(ofd.FileName);
                    var backupObj = JsonConvert.DeserializeObject<dynamic>(jsonContent);

                    await ApiService.PostAsync<dynamic>("settings/restore", backupObj);
                    
                    MessageBox.Show("System successfully restored. Please restart the application.", "Restore Complete", MessageBoxButton.OK, MessageBoxImage.Information);
                    AuditService.Log("Database Restore", $"Imported system state from {Path.GetFileName(ofd.FileName)}");
                    
                    await DataStore.LoadAsync(); // Refresh local memory
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Restore failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void AddUser(object parameter)
        {
            var passwordBox = parameter as System.Windows.Controls.PasswordBox;
            string password = passwordBox?.Password;

            if (string.IsNullOrWhiteSpace(FormUsername) || string.IsNullOrWhiteSpace(password))
            {
                MessageBox.Show("Username and Password are required.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (DataStore.Data.Users.Any(u => u.Username.Equals(FormUsername, StringComparison.OrdinalIgnoreCase)))
            {
                MessageBox.Show("Username already exists.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var newUser = new UserModel
            {
                Id = Guid.NewGuid().ToString(),
                Username = FormUsername,
                Password = password,
                Role = FormRole
            };

            try
            {
                await ApiService.PostAsync<UserModel>("users", newUser);
                await DataStore.LoadAsync();
                UsersList = new ObservableCollection<UserModel>(DataStore.Data.Users);
                OnPropertyChanged(nameof(UsersList));
                
                MessageBox.Show($"Staff account '{FormUsername}' created successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                AuditService.Log("User Created", $"Created new staff account for {FormUsername} ({FormRole}).");

                // Clear form
                FormUsername = "";
                if (passwordBox != null) passwordBox.Password = "";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error creating user: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // --- Validation Logic ---
        protected override void ValidateProperty(string propertyName)
        {
            RemoveError(propertyName);

            switch (propertyName)
            {
                case nameof(HotelName):
                    if (string.IsNullOrWhiteSpace(HotelName))
                        AddError(propertyName, "Hotel Name is required.");
                    break;

                case nameof(HotelEmail):
                    if (!string.IsNullOrWhiteSpace(HotelEmail) && !System.Text.RegularExpressions.Regex.IsMatch(HotelEmail, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
                        AddError(propertyName, "Invalid email format.");
                    break;

                case nameof(FormUsername):
                    if (string.IsNullOrWhiteSpace(FormUsername))
                        AddError(propertyName, "Username is required.");
                    else if (FormUsername.Length < 3)
                        AddError(propertyName, "Username must be at least 3 characters.");
                    break;

                case nameof(TaxRate):
                    if (TaxRate < 0 || TaxRate > 100)
                        AddError(propertyName, "Tax rate must be between 0 and 100.");
                    break;
            }
        }

        private void ExportData()
        {
            if (!ExportRooms && !ExportGuests && !ExportReservations)
            {
                MessageBox.Show("Please select at least one item to export.", "Selection Required", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            SaveFileDialog sfd = new SaveFileDialog
            {
                Filter = "Text Files (*.txt)|*.txt|CSV Files (*.csv)|*.csv",
                FileName = $"Hotel_Export_{DateTime.Now:yyyyMMdd}.txt",
                Title = "Export System Data"
            };

            if (sfd.ShowDialog() == true)
            {
                try
                {
                    var sb = new System.Text.StringBuilder();
                    sb.AppendLine("=================================================================================");
                    sb.AppendLine($"               HOTEL DATA EXPORT - {HotelName?.ToUpper()}");
                    sb.AppendLine($"               Generated on: {DateTime.Now}");
                    sb.AppendLine("=================================================================================");
                    sb.AppendLine();

                    if (ExportRooms)
                    {
                        sb.AppendLine("--- ROOMS INVENTORY ---");
                        sb.AppendLine(string.Format("{0,-10} | {1,-15} | {2,-10} | {3,-10} | {4,-15}", "Number", "Type", "Floor", "Price", "Status"));
                        sb.AppendLine(new string('-', 70));
                        foreach (var r in DataStore.Data.Rooms)
                        {
                            var type = DataStore.Data.RoomTypes.FirstOrDefault(t => t.Id == r.TypeId)?.Name ?? "Unknown";
                            sb.AppendLine(string.Format("{0,-10} | {1,-15} | {2,-10} | {3,-10:C} | {4,-15}", r.RoomNumber, type, r.FloorNumber, r.BasePricePerNight, r.Status));
                        }
                        sb.AppendLine();
                    }

                    if (ExportGuests)
                    {
                        sb.AppendLine("--- GUEST DATABASE ---");
                        sb.AppendLine(string.Format("{0,-20} | {1,-15} | {2,-25} | {3,-10}", "Full Name", "Phone", "Email", "Status"));
                        sb.AppendLine(new string('-', 80));
                        foreach (var g in DataStore.Data.Customers)
                        {
                            string status = g.IsBlacklisted ? "BLACKLISTED" : "REGULAR";
                            sb.AppendLine(string.Format("{0,-20} | {1,-15} | {2,-25} | {3,-10}", g.FullName, g.Phone, g.Email ?? "N/A", status));
                        }
                        sb.AppendLine();
                    }

                    if (ExportReservations)
                    {
                        sb.AppendLine("--- RESERVATIONS LOG ---");
                        if (ExportStartDate.HasValue || ExportEndDate.HasValue)
                        {
                            sb.AppendLine($"Filter: {ExportStartDate?.ToShortDateString() ?? "Start"} to {ExportEndDate?.ToShortDateString() ?? "End"}");
                        }
                        sb.AppendLine(string.Format("{0,-10} | {1,-20} | {2,-10} | {3,-12} | {4,-12} | {5,-10}", "ID", "Guest", "Room", "Check-In", "Check-Out", "Total"));
                        sb.AppendLine(new string('-', 90));
                        
                        var resList = DataStore.Data.Reservations.AsEnumerable();
                        if (ExportStartDate.HasValue) resList = resList.Where(r => r.CheckIn >= ExportStartDate.Value);
                        if (ExportEndDate.HasValue) resList = resList.Where(r => r.CheckIn <= ExportEndDate.Value);

                        foreach (var r in resList)
                        {
                            var guest = DataStore.Data.Customers.FirstOrDefault(c => c.Id == r.CustomerId)?.FullName ?? "Unknown";
                            var room = DataStore.Data.Rooms.FirstOrDefault(rm => rm.Id == r.RoomId)?.RoomNumber ?? "N/A";
                            sb.AppendLine(string.Format("{0,-10} | {1,-20} | {2,-10} | {3,-12:yyyy-MM-dd} | {4,-12:yyyy-MM-dd} | {5,-10:C}", r.Id.Substring(0,Math.Min(r.Id.Length,8)), guest, room, r.CheckIn, r.CheckOut, r.TotalPrice));
                        }
                        sb.AppendLine();
                    }

                    File.WriteAllText(sfd.FileName, sb.ToString());
                    AuditService.Log("Data Exported", $"Exported selected system data to {Path.GetFileName(sfd.FileName)}");
                    MessageBox.Show("Data successfully exported!", "Export Complete", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Export failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async void DeleteSelectedUser()
        {
            if (SelectedUser == null)
            {
                MessageBox.Show("Please select a user to delete.", "Selection Required", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (SelectedUser.Username == "admin")
            {
                MessageBox.Show("The primary administrator account cannot be deleted.", "Action Restricted", MessageBoxButton.OK, MessageBoxImage.Stop);
                return;
            }

            var result = MessageBox.Show($"Are you sure you want to delete staff account '{SelectedUser.Username}'?", "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    await ApiService.DeleteAsync($"users/{SelectedUser.Id}");
                    await DataStore.LoadAsync();
                    UsersList = new ObservableCollection<UserModel>(DataStore.Data.Users);
                    OnPropertyChanged(nameof(UsersList));

                    AuditService.Log("User Deleted", $"Deleted staff account for {SelectedUser.Username}.");
                    SelectedUser = null;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error deleting user: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}
