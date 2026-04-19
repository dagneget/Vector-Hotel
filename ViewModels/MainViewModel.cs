using System.Windows.Input;
using HRS.Services;

namespace HRS.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private ViewModelBase _currentViewModel;
        public ViewModelBase CurrentViewModel
        {
            get => _currentViewModel;
            set => SetProperty(ref _currentViewModel, value);
        }

        public string CurrentUserName => AuthService.CurrentUser?.Username?.ToUpper() ?? "UNKNOWN";
        public string CurrentUserRole => AuthService.CurrentUser?.Role?.ToUpper() ?? "GUEST";

        public bool IsAdmin => AuthService.IsAdmin();
        public bool IsReceptionist => AuthService.IsReceptionist();
        public bool IsAccountant => AuthService.IsAccountant();

        public bool CanViewReservations => IsAdmin || IsReceptionist;
        public bool CanViewCustomers => IsAdmin || IsReceptionist;
        public bool CanViewRooms => IsAdmin || IsReceptionist;
        public bool CanViewPayments => true; 
        public bool CanViewReports => IsAdmin || IsAccountant;
        public bool CanViewSettings => IsAdmin;

        public ICommand NavigateDashboardCommand { get; }
        public ICommand NavigateReservationsCommand { get; }
        public ICommand NavigateRoomsCommand { get; }
        public ICommand NavigateCustomersCommand { get; }
        public ICommand NavigatePaymentsCommand { get; }
        public ICommand NavigateReportsCommand { get; }
        public ICommand NavigateSettingsCommand { get; }
        public ICommand LogoutCommand { get; }

        public MainViewModel()
        {
            // Default Start Page based on role
            if (IsAccountant) CurrentViewModel = new PaymentsViewModel();
            else CurrentViewModel = new DashboardViewModel();

            NavigateDashboardCommand    = new RelayCommand(_ => CurrentViewModel = new DashboardViewModel()); 
            NavigateReservationsCommand = new RelayCommand(_ => CurrentViewModel = new ReservationsViewModel()); 
            NavigateRoomsCommand        = new RelayCommand(_ => CurrentViewModel = new RoomsViewModel()); 
            NavigateCustomersCommand    = new RelayCommand(_ => CurrentViewModel = new CustomersViewModel()); 
            NavigatePaymentsCommand     = new RelayCommand(_ => CurrentViewModel = new PaymentsViewModel()); 
            NavigateReportsCommand      = new RelayCommand(_ => CurrentViewModel = new ReportsViewModel());
            NavigateSettingsCommand     = new RelayCommand(_ => CurrentViewModel = new SettingsViewModel());
            LogoutCommand               = new RelayCommand(_ => Logout());
        }

        private void Logout()
        {
            AuthService.Logout();
            var loginView = new Views.LoginView();
            loginView.Show();
            
            foreach (System.Windows.Window window in System.Windows.Application.Current.Windows)
            {
                if (window is Views.MainDashboard)
                {
                    window.Close();
                    break;
                }
            }
        }
    }
}
