using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
using HRS.Models;
using HRS.Services;

namespace HRS.ViewModels
{
    public class ProfileViewModel : ViewModelBase
    {
        private string _username;
        private string _role;
        private string _currentPassword;
        private string _newPassword;
        private string _confirmPassword;
        private bool _isUpdating;

        public string Username
        {
            get => _username;
            set => SetProperty(ref _username, value);
        }

        public string Role
        {
            get => _role;
            set => SetProperty(ref _role, value);
        }

        public string CurrentPassword
        {
            get => _currentPassword;
            set => SetProperty(ref _currentPassword, value);
        }

        public string NewPassword
        {
            get => _newPassword;
            set => SetProperty(ref _newPassword, value);
        }

        public string ConfirmPassword
        {
            get => _confirmPassword;
            set => SetProperty(ref _confirmPassword, value);
        }

        public bool IsUpdating
        {
            get => _isUpdating;
            set => SetProperty(ref _isUpdating, value);
        }

        public ICommand UpdatePasswordCommand { get; }

        public ProfileViewModel()
        {
            Username = AuthService.CurrentUser?.Username ?? "Unknown";
            Role = AuthService.CurrentUser?.Role ?? "Unknown";

            UpdatePasswordCommand = new RelayCommand(UpdatePassword);
        }

        private async void UpdatePassword(object parameter)
        {
            // Use PasswordBoxes if provided via CommandParameter (as a Tuple or custom element)
            // But we can also just use data bindings or pass the PasswordBoxes as parameters!
            // Let's pass the window or the password boxes.
            // Best WPF practice with PasswordBox is passing them as parameters.

            var passwordContainer = parameter as object[];
            if (passwordContainer != null && passwordContainer.Length >= 3)
            {
                var pbCurrent = passwordContainer[0] as PasswordBox;
                var pbNew = passwordContainer[1] as PasswordBox;
                var pbConfirm = passwordContainer[2] as PasswordBox;

                CurrentPassword = pbCurrent?.Password;
                NewPassword = pbNew?.Password;
                ConfirmPassword = pbConfirm?.Password;
            }

            if (string.IsNullOrWhiteSpace(CurrentPassword) || 
                string.IsNullOrWhiteSpace(NewPassword) || 
                string.IsNullOrWhiteSpace(ConfirmPassword))
            {
                MessageBox.Show("Please fill in all password fields.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (NewPassword != ConfirmPassword)
            {
                MessageBox.Show("New passwords do not match.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            IsUpdating = true;
            try
            {
                // Verify old password by attempting a dummy login against the API
                var isValid = await AuthService.LoginAsync(Username, CurrentPassword);
                if (!isValid)
                {
                    MessageBox.Show("Current password verification failed.", "Access Denied", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Old password is valid! Let's update the user record
                var userToUpdate = AuthService.CurrentUser;
                userToUpdate.Password = NewPassword;

                await ApiService.PutAsync($"users/{userToUpdate.Id}", userToUpdate);
                AuditService.Log("Profile Updated", $"User '{Username}' modified their own password.");

                MessageBox.Show("Password updated successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                
                // Clear inputs
                if (passwordContainer != null)
                {
                    ((PasswordBox)passwordContainer[0]).Clear();
                    ((PasswordBox)passwordContainer[1]).Clear();
                    ((PasswordBox)passwordContainer[2]).Clear();
                }
                CurrentPassword = "";
                NewPassword = "";
                ConfirmPassword = "";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to update password: {ex.Message}", "System Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsUpdating = false;
            }
        }
    }
}
