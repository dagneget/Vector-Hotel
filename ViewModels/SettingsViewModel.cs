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
        public ObservableCollection<AuditLogModel> AuditLogsList { get; set; }

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

        public SettingsViewModel()
        {
            UsersList = new ObservableCollection<UserModel>(DataStore.Data.Users);
            AuditLogsList = new ObservableCollection<AuditLogModel>(DataStore.Data.AuditLogs.OrderByDescending(log => log.Timestamp));
            
            SaveSettingsCommand = new RelayCommand(_ => SaveSettings());
            BackupCommand = new RelayCommand(_ => BackupDatabase());
            RestoreCommand = new RelayCommand(_ => RestoreDatabase());
            AddUserCommand = new RelayCommand(AddUser);
            DeleteUserCommand = new RelayCommand(_ => DeleteSelectedUser());

            FormRole = RoleOptions.FirstOrDefault();

            LoadSettings();
            AuditService.Log("Accessed Settings", "Admin accessed system settings and audit logs.");
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
                // Backup returns JSON. Let's use JToken to capture any schema
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
