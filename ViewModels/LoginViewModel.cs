using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Threading.Tasks;
using HRS.Services;
using HRS.Views;

namespace HRS.ViewModels
{
    public class LoginViewModel : ViewModelBase
    {
        private string _username;
        public string Username { get => _username; set => SetProperty(ref _username, value); }
        
        private string _errorMessage;
        public string ErrorMessage { get => _errorMessage; set => SetProperty(ref _errorMessage, value); }
        
        public ICommand LoginCommand { get; }
        public ICommand ExitCommand { get; }

        public LoginViewModel()
        {
            LoginCommand = new RelayCommand(async p => await Login(p));
            ExitCommand = new RelayCommand(_ => Application.Current.Shutdown());
        }

        private async Task Login(object parameter)
        {
            string password = "";
            if (parameter is PasswordBox pb)
            {
                password = pb.Password;
            }

            if (string.IsNullOrWhiteSpace(Username))
            {
                ErrorMessage = "Username is required.";
                return;
            }

            ErrorMessage = "Authenticating...";
            
            try 
            {
                if (await AuthService.LoginAsync(Username, password))
                {
                    AuditService.Log("User Login", $"User '{Username}' logged into the terminal.");
                    var mainDashboard = new MainDashboard();
                    mainDashboard.Show();
                    
                    // Close login window
                    foreach (Window w in Application.Current.Windows)
                    {
                        if (w is LoginView)
                        {
                            w.Close();
                            break;
                        }
                    }
                }
                else
                {
                    ErrorMessage = "Invalid username or password.";
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Server Error: {ex.Message}";
                if (ex.InnerException != null) ErrorMessage += $" ({ex.InnerException.Message})";
                
                try
                {
                    System.IO.File.WriteAllText(@"c:\Users\SW\OneDrive\Documents\Hotel-Reservation-System-HRS\error.log", ex.ToString());
                }
                catch { }
                
                Console.WriteLine(ex.ToString());
            }
        }
    }
}
