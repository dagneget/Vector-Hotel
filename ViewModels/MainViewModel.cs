using System.Windows.Input;
using HRS.Services;
using MaterialDesignThemes.Wpf;

namespace HRS.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private ViewModelBase _currentViewModel;
        public ViewModelBase CurrentViewModel
        {
            get => _currentViewModel;
            set
            {
                if (SetProperty(ref _currentViewModel, value))
                {
                    // Notify all active state properties changed
                    OnPropertyChanged(nameof(IsDashboardActive));
                    OnPropertyChanged(nameof(IsReservationsActive));
                    OnPropertyChanged(nameof(IsRoomsActive));
                    OnPropertyChanged(nameof(IsCustomersActive));
                    OnPropertyChanged(nameof(IsPaymentsActive));
                    OnPropertyChanged(nameof(IsReportsActive));
                    OnPropertyChanged(nameof(IsSettingsActive));
                    OnPropertyChanged(nameof(IsAuditLogsActive));
                }
            }
        }

        // Active view indicators for sidebar highlighting
        public bool IsDashboardActive => CurrentViewModel is DashboardViewModel;
        public bool IsReservationsActive => CurrentViewModel is ReservationsViewModel;
        public bool IsRoomsActive => CurrentViewModel is RoomsViewModel;
        public bool IsCustomersActive => CurrentViewModel is CustomersViewModel;
        public bool IsPaymentsActive => CurrentViewModel is PaymentsViewModel;
        public bool IsReportsActive => CurrentViewModel is ReportsViewModel;
        public bool IsSettingsActive => CurrentViewModel is SettingsViewModel;
        public bool IsAuditLogsActive => CurrentViewModel is AuditLogsViewModel;

        public string CurrentUserName => AuthService.CurrentUser?.Username?.ToUpper() ?? "UNKNOWN";
        public string CurrentUserRole => AuthService.CurrentUser?.Role?.ToUpper() ?? "GUEST";

        private bool _isNotificationsOpen;
        public bool IsNotificationsOpen
        {
            get => _isNotificationsOpen;
            set { _isNotificationsOpen = value; OnPropertyChanged(nameof(IsNotificationsOpen)); }
        }

        public System.Collections.ObjectModel.ObservableCollection<Models.AuditLogModel> RecentNotifications => DataStore.Data.AuditLogs;

        // Theme Properties
        public bool IsDarkMode => ThemeManager.IsDarkMode;
        public PackIconKind ThemeIcon => IsDarkMode ? PackIconKind.WeatherSunny : PackIconKind.MoonWaningCrescent;
        public string ThemeTooltip => IsDarkMode ? "Switch to Light Mode" : "Switch to Dark Mode";

        public bool IsAdmin => AuthService.IsAdmin();
        public bool IsReceptionist => AuthService.IsReceptionist();
        public bool IsAccountant => AuthService.IsAccountant();

        public bool CanViewReservations => IsAdmin || IsReceptionist || IsAccountant;
        public bool CanViewCustomers => IsAdmin || IsReceptionist || IsAccountant;
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
        public ICommand NavigateAuditLogsCommand { get; }
        public ICommand LogoutCommand { get; }
        public ICommand ToggleThemeCommand { get; }
        public ICommand ToggleNotificationsCommand { get; }
        public ICommand OpenProfileCommand { get; }

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
            NavigateAuditLogsCommand    = new RelayCommand(_ => CurrentViewModel = new AuditLogsViewModel());
            LogoutCommand               = new RelayCommand(_ => Logout());
            ToggleThemeCommand          = new RelayCommand(_ => ToggleTheme());
            ToggleNotificationsCommand  = new RelayCommand(_ => IsNotificationsOpen = !IsNotificationsOpen);
            OpenProfileCommand          = new RelayCommand(_ => OpenProfileDialog());

            // Subscribe to theme changes
            ThemeManager.ThemeChanged += isDark =>
            {
                OnPropertyChanged(nameof(IsDarkMode));
                OnPropertyChanged(nameof(ThemeIcon));
                OnPropertyChanged(nameof(ThemeTooltip));
            };

            // Dashboard "VIEW ALL" button fires this event to navigate here
            EventBus.Instance.NavigateToReservationsRequested += () =>
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    CurrentViewModel = new ReservationsViewModel());

            // Dashboard FAB button fires this event to create new reservation
            EventBus.Instance.NewReservationRequested += () =>
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    var vm = new ReservationsViewModel();
                    CurrentViewModel = vm;
                    vm.TriggerNewReservation();
                });

            // Handle navigation to specific customer from detail popup
            EventBus.Instance.NavigateToCustomerRequested += customerId =>
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    var vm = new CustomersViewModel();
                    CurrentViewModel = vm;
                    vm.SelectCustomerById(customerId);
                });

            // Handle navigation to specific room from detail popup
            EventBus.Instance.NavigateToRoomRequested += roomId =>
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    var vm = new RoomsViewModel();
                    CurrentViewModel = vm;
                    vm.SelectRoomById(roomId);
                });
        }

        private void ToggleTheme()
        {
            ThemeManager.ToggleTheme();
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

        private void OpenProfileDialog()
        {
            var profileWindow = new Views.ProfileWindow();
            profileWindow.ShowDialog();
        }
    }
}
