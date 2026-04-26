using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using System.Windows.Media;
using HRS.Models;
using HRS.Services;

namespace HRS.ViewModels
{
    public class FeedItem
    {
        public string IconName { get; set; }
        public string Title { get; set; }
        public string SubText { get; set; }
        public string StatusColor { get; set; }
    }

    public class DashboardViewModel : ViewModelBase
    {
        // Headers linked to real data
        public string OccupancyText 
        {
            get {
                var totalRooms = DataStore.Data.Rooms.Count();
                if (totalRooms == 0) return "0%";
                var occupied = DataStore.Data.Rooms.Count(r => r.Status == "Occupied" || r.Status == "Reserved");
                return $"{(occupied * 100.0 / totalRooms):F1}%";
            }
        }

        public string AdrText 
        {
            get {
                var activeRes = DataStore.Data.Reservations.Where(r => r.RoomStatus == "CheckedIn" || r.RoomStatus == "CheckedOut").ToList();
                if (!activeRes.Any()) return "$0";
                return $"${activeRes.Average(r => r.TotalPrice):F0}";
            }
        }

        public string RevparText 
        {
            get {
                var totalRooms = DataStore.Data.Rooms.Count();
                if (totalRooms == 0) return "$0";
                var totalRev = DataStore.Data.Reservations.Where(r => r.RoomStatus == "CheckedIn" || r.RoomStatus == "CheckedOut").Sum(r => r.TotalPrice);
                return $"${(totalRev / totalRooms):F0}";
            }
        }

        public string ArrivalsText => DataStore.Data.Reservations.Count(r => r.CheckIn.Date == DateTime.Today).ToString();

        // Real trend analysis properties
        public string OccupancyTrendText
        {
            get
            {
                var today = DateTime.Today;
                var lastWeek = today.AddDays(-7);
                
                var totalRooms = DataStore.Data.Rooms.Count();
                if (totalRooms == 0) return "No data";
                
                // Current occupancy
                var currentOccupied = DataStore.Data.Rooms.Count(r => r.Status == "Occupied" || r.Status == "Reserved");
                var currentRate = (double)currentOccupied / totalRooms;
                
                // Last week occupancy (based on reservations that were active last week)
                var lastWeekCheckins = DataStore.Data.Reservations
                    .Where(r => r.CheckIn.Date <= lastWeek && r.CheckOut.Date >= lastWeek)
                    .Count();
                var lastWeekRate = Math.Min((double)lastWeekCheckins / totalRooms, 1.0);
                
                var change = (currentRate - lastWeekRate) * 100;
                var sign = change >= 0 ? "+" : "";
                return $"{sign}{change:F1}% vs last week";
            }
        }

        public Brush OccupancyTrendBrush => 
            DataStore.Data.Rooms.Count(r => r.Status == "Occupied" || r.Status == "Reserved") >= 
            DataStore.Data.Reservations.Where(r => r.CheckIn.Date <= DateTime.Today.AddDays(-7) && r.CheckOut.Date >= DateTime.Today.AddDays(-7)).Count() 
            ? (Brush)new SolidColorBrush(Color.FromRgb(76, 175, 80)) // SuccessGreen
            : (Brush)new SolidColorBrush(Color.FromRgb(244, 67, 54)); // DangerRed

        public string AdrTrendText
        {
            get
            {
                var currentMonth = DateTime.Today.Month;
                var currentYear = DateTime.Today.Year;
                
                // Current month ADR
                var currentReservations = DataStore.Data.Reservations
                    .Where(r => r.CheckIn.Month == currentMonth && r.CheckIn.Year == currentYear && 
                               (r.RoomStatus == "CheckedIn" || r.RoomStatus == "CheckedOut"))
                    .ToList();
                
                if (!currentReservations.Any()) return "No data";
                var currentAdr = currentReservations.Average(r => r.TotalPrice);
                
                // Previous month ADR for comparison
                var prevMonth = currentMonth == 1 ? 12 : currentMonth - 1;
                var prevYear = currentMonth == 1 ? currentYear - 1 : currentYear;
                var prevReservations = DataStore.Data.Reservations
                    .Where(r => r.CheckIn.Month == prevMonth && r.CheckIn.Year == prevYear && 
                               (r.RoomStatus == "CheckedIn" || r.RoomStatus == "CheckedOut"))
                    .ToList();
                
                if (!prevReservations.Any()) return $"${currentAdr:F0} this month";
                var prevAdr = prevReservations.Average(r => r.TotalPrice);
                
                var change = ((currentAdr - prevAdr) / prevAdr) * 100;
                var sign = change >= 0 ? "+" : "";
                var label = change >= 0 ? "vs last month" : "from last month";
                return $"{sign}{change:F0}% {label}";
            }
        }

        public Brush AdrTrendBrush =>
            (DataStore.Data.Reservations.Where(r => r.CheckIn.Month == DateTime.Today.Month && 
                (r.RoomStatus == "CheckedIn" || r.RoomStatus == "CheckedOut")).DefaultIfEmpty().Average(r => r?.TotalPrice ?? 0)) >=
            (DataStore.Data.Reservations.Where(r => r.CheckIn.Month == (DateTime.Today.Month == 1 ? 12 : DateTime.Today.Month - 1) && 
                (r.RoomStatus == "CheckedIn" || r.RoomStatus == "CheckedOut")).DefaultIfEmpty().Average(r => r?.TotalPrice ?? 0))
            ? (Brush)new SolidColorBrush(Color.FromRgb(76, 175, 80)) // SuccessGreen
            : (Brush)new SolidColorBrush(Color.FromRgb(244, 67, 54)); // DangerRed

        public string RevparTrendText
        {
            get
            {
                var totalRooms = DataStore.Data.Rooms.Count();
                if (totalRooms == 0) return "No data";
                
                // Current RevPAR
                var currentRev = DataStore.Data.Reservations
                    .Where(r => r.RoomStatus == "CheckedIn" || r.RoomStatus == "CheckedOut")
                    .Sum(r => r.TotalPrice);
                var currentRevpar = (double)currentRev / totalRooms;
                
                // Target RevPAR (estimate based on room type rates)
                var avgRoomRate = DataStore.Data.RoomTypes.Any() ? 
                    (double)DataStore.Data.RoomTypes.Average(rt => rt.BasePrice) : 0;
                var targetRevpar = avgRoomRate * 0.7; // Assume 70% occupancy target
                
                if (targetRevpar == 0) return $"${currentRevpar:F0} revenue";
                
                var variance = ((currentRevpar - targetRevpar) / targetRevpar) * 100;
                var sign = variance >= 0 ? "+" : "";
                return $"{sign}{variance:F1}% target variance";
            }
        }

        public Brush RevparTrendBrush =>
            (DataStore.Data.Reservations.Where(r => r.RoomStatus == "CheckedIn" || r.RoomStatus == "CheckedOut").Sum(r => r.TotalPrice) / 
             Math.Max(DataStore.Data.Rooms.Count(), 1)) >=
            (DataStore.Data.RoomTypes.Any() ? DataStore.Data.RoomTypes.Average(rt => rt.BasePrice) * 0.7m : 0m)
            ? (Brush)new SolidColorBrush(Color.FromRgb(76, 175, 80)) // SuccessGreen
            : (Brush)new SolidColorBrush(Color.FromRgb(244, 67, 54)); // DangerRed

        public string ActiveCheckinsText
        {
            get
            {
                var active = DataStore.Data.Reservations.Count(r => 
                    r.RoomStatus == "CheckedIn" && r.CheckIn.Date == DateTime.Today);
                return $"{active} Active Check-ins";
            }
        }

        private ObservableCollection<ReservationDisplayModel> _recentReservations;
        public ObservableCollection<ReservationDisplayModel> RecentReservations
        {
            get => _recentReservations;
            set => SetProperty(ref _recentReservations, value);
        }

        // --- Filter State ---
        private bool _isFilterOpen;
        public bool IsFilterOpen
        {
            get => _isFilterOpen;
            set => SetProperty(ref _isFilterOpen, value);
        }

        private string _filterStatus = "All";
        public string FilterStatus
        {
            get => _filterStatus;
            set { if (SetProperty(ref _filterStatus, value)) LoadData(); }
        }

        public string[] FilterStatusOptions => new[] { "All", "Confirmed", "Pending", "CheckedIn", "CheckedOut", "Cancelled" };

        // --- Commands ---
        public ICommand ViewAllCommand { get; }
        public ICommand ToggleFilterCommand { get; }
        public ICommand ApplyFilterCommand { get; }
        public ICommand ViewOccupancyDetailsCommand { get; }
        public ICommand ViewAdrDetailsCommand { get; }
        public ICommand ViewRevparDetailsCommand { get; }
        public ICommand ViewArrivalsDetailsCommand { get; }
        public ICommand CloseDetailsCommand { get; }
        public ICommand FilterDetailsCommand { get; }
        public ICommand ViewOccupiedRoomsCommand { get; }
        public ICommand ViewAvailableRoomsCommand { get; }
        public ICommand ViewMaintenanceRoomsCommand { get; }
        public ICommand ViewAllRoomsCommand { get; }
        public ICommand QuickAddCommand { get; }
        public ICommand ViewCustomerDetailsCommand { get; }
        public ICommand ViewRoomDetailsCommand { get; }

        // --- Detail View State ---
        private bool _isDetailsOpen;
        public bool IsDetailsOpen
        {
            get => _isDetailsOpen;
            set => SetProperty(ref _isDetailsOpen, value);
        }

        private string _detailTitle;
        public string DetailTitle
        {
            get => _detailTitle;
            set => SetProperty(ref _detailTitle, value);
        }

        private string _detailType;
        public string DetailType
        {
            get => _detailType;
            set => SetProperty(ref _detailType, value);
        }

        private ObservableCollection<ReservationDisplayModel> _detailItems;
        public ObservableCollection<ReservationDisplayModel> DetailItems
        {
            get => _detailItems;
            set => SetProperty(ref _detailItems, value);
        }

        private string _detailFilter = "All";
        public string DetailFilter
        {
            get => _detailFilter;
            set { if (SetProperty(ref _detailFilter, value)) RefreshDetailView(); }
        }

        public string[] DetailFilterOptions => new[] { "All", "CheckedIn", "CheckedOut", "Confirmed", "Pending", "Cancelled" };

        // --- Date Range Filter ---
        private DateTime? _filterStartDate;
        public DateTime? FilterStartDate
        {
            get => _filterStartDate;
            set { if (SetProperty(ref _filterStartDate, value)) RefreshDetailView(); }
        }

        private DateTime? _filterEndDate;
        public DateTime? FilterEndDate
        {
            get => _filterEndDate;
            set { if (SetProperty(ref _filterEndDate, value)) RefreshDetailView(); }
        }

        public string[] TimeFilterOptions => new[] { "All Time", "Last 7 Days", "Last 30 Days", "This Month", "Last Month", "This Year", "Custom Range" };

        private string _selectedTimeFilter = "All Time";
        public string SelectedTimeFilter
        {
            get => _selectedTimeFilter;
            set { if (SetProperty(ref _selectedTimeFilter, value)) ApplyTimeFilter(); }
        }

        private ObservableCollection<FeedItem> _frontDeskFeed;
        public ObservableCollection<FeedItem> FrontDeskFeed
        {
            get => _frontDeskFeed;
            set => SetProperty(ref _frontDeskFeed, value);
        }

        public int TotalRooms => DataStore.Data.Rooms.Count();
        public int OccupiedRooms => DataStore.Data.Rooms.Count(r => r.Status == "Occupied" || r.Status == "Reserved");
        public int MaintenanceRooms => DataStore.Data.Rooms.Count(r => (r.Status != "Occupied" && r.Status != "Reserved") && (r.CleanStatus == "Maintenance" || r.CleanStatus == "Dirty" || r.Status == "OutOfOrder"));
        public int AvailableRooms => TotalRooms - OccupiedRooms - MaintenanceRooms;

        // Pie chart calculation
        private const double CircumferenceUnits = Math.PI * 9.0;

        public string OccupiedDashArray => $"{((TotalRooms > 0 ? (double)OccupiedRooms / TotalRooms : 0) * CircumferenceUnits).ToString(System.Globalization.CultureInfo.InvariantCulture)} 1000";
        public double OccupiedDashOffset => 0;

        public string AvailableDashArray => $"{((TotalRooms > 0 ? (double)AvailableRooms / TotalRooms : 0) * CircumferenceUnits).ToString(System.Globalization.CultureInfo.InvariantCulture)} 1000";
        public double AvailableDashOffset => -((TotalRooms > 0 ? (double)OccupiedRooms / TotalRooms : 0) * CircumferenceUnits);

        public string MaintenanceDashArray => $"{((TotalRooms > 0 ? (double)MaintenanceRooms / TotalRooms : 0) * CircumferenceUnits).ToString(System.Globalization.CultureInfo.InvariantCulture)} 1000";
        public double MaintenanceDashOffset => -((TotalRooms > 0 ? (double)(OccupiedRooms + AvailableRooms) / TotalRooms : 0) * CircumferenceUnits);

        public DashboardViewModel()
        {
            ViewAllCommand      = new RelayCommand(_ => EventBus.Instance.PublishNavigateToReservations());
            ToggleFilterCommand = new RelayCommand(_ => IsFilterOpen = !IsFilterOpen);
            ApplyFilterCommand  = new RelayCommand(_ => { LoadData(); IsFilterOpen = false; });
            
            ViewOccupancyDetailsCommand = new RelayCommand(_ => ShowOccupancyDetails());
            ViewAdrDetailsCommand = new RelayCommand(_ => ShowAdrDetails());
            ViewRevparDetailsCommand = new RelayCommand(_ => ShowRevparDetails());
            ViewArrivalsDetailsCommand = new RelayCommand(_ => ShowArrivalsDetails());
            CloseDetailsCommand = new RelayCommand(_ => IsDetailsOpen = false);
            FilterDetailsCommand = new RelayCommand(filter => { DetailFilter = filter?.ToString() ?? "All"; });
            
            ViewOccupiedRoomsCommand = new RelayCommand(_ => ShowRoomDetails("Occupied"));
            ViewAvailableRoomsCommand = new RelayCommand(_ => ShowRoomDetails("Available"));
            ViewMaintenanceRoomsCommand = new RelayCommand(_ => ShowRoomDetails("Maintenance"));
            ViewAllRoomsCommand = new RelayCommand(_ => ShowRoomDetails("All"));
            QuickAddCommand = new RelayCommand(_ => EventBus.Instance.PublishNewReservation());
            ViewCustomerDetailsCommand = new RelayCommand(item => ShowCustomerDetails(item));
            ViewRoomDetailsCommand = new RelayCommand(item => ShowRoomDetails(item));

            LoadData();
            EventBus.Instance.DataChanged += () => LoadData();
        }

        private void ShowOccupancyDetails()
        {
            DetailType = "Occupancy";
            DetailTitle = "Occupancy Details - All Reservations";
            IsDetailsOpen = true;
            SelectedTimeFilter = "All Time";
            RefreshDetailView();
        }

        private void ShowAdrDetails()
        {
            DetailType = "ADR";
            DetailTitle = "Average Daily Rate Analysis";
            IsDetailsOpen = true;
            RefreshDetailView();
        }

        private void ShowRevparDetails()
        {
            DetailType = "RevPAR";
            DetailTitle = "Revenue Per Available Room";
            IsDetailsOpen = true;
            RefreshDetailView();
        }

        private void ShowArrivalsDetails()
        {
            DetailType = "Arrivals";
            DetailTitle = "Today's Arrivals & Check-ins";
            IsDetailsOpen = true;
            RefreshDetailView();
        }

        private void ShowRoomDetails(string roomStatus)
        {
            DetailType = "Rooms";
            DetailTitle = roomStatus == "All" ? "All Rooms" : $"{roomStatus} Rooms";
            _roomFilterStatus = roomStatus;
            IsDetailsOpen = true;
            RefreshRoomDetailView();
        }

        private string _roomFilterStatus = "All";

        private void RefreshRoomDetailView()
        {
            IEnumerable<RoomDetailItem> query = DataStore.Data.Rooms.Select(r =>
            {
                var roomType = DataStore.Data.RoomTypes.FirstOrDefault(rt => rt.Id == r.TypeId);
                var reservation = DataStore.Data.Reservations.FirstOrDefault(res => 
                    res.RoomId == r.Id && 
                    res.CheckIn.Date <= DateTime.Today && 
                    res.CheckOut.Date >= DateTime.Today);
                var customer = reservation != null ? 
                    DataStore.Data.Customers.FirstOrDefault(c => c.Id == reservation.CustomerId) : null;
                
                return new RoomDetailItem
                {
                    Room = r,
                    RoomType = roomType?.Name ?? "Unknown",
                    BasePrice = roomType?.BasePrice ?? 0,
                    CurrentGuest = customer?.FullName,
                    CurrentReservation = reservation,
                    DisplayStatus = GetRoomDisplayStatus(r)
                };
            });

            if (_roomFilterStatus != "All")
                query = query.Where(r => r.DisplayStatus == _roomFilterStatus);

            DetailRooms = new ObservableCollection<RoomDetailItem>(query.OrderBy(r => r.Room.RoomNumber));
            OnPropertyChanged(nameof(DetailRooms));
        }

        private string GetRoomDisplayStatus(RoomModel room)
        {
            if (room.Status == "Occupied" || room.Status == "Reserved") return "Occupied";
            if (room.CleanStatus == "Maintenance" || room.CleanStatus == "Dirty" || room.Status == "OutOfOrder") return "Maintenance";
            return "Available";
        }

        private void ShowCustomerDetails(object item)
        {
            if (item is ReservationDisplayModel reservation)
            {
                // Close the current details popup
                IsDetailsOpen = false;
                
                // Navigate to customers and pass the selected customer info
                EventBus.Instance.PublishNavigateToCustomer(reservation.BaseReservation.CustomerId);
            }
        }

        private void ShowRoomDetails(object item)
        {
            if (item is RoomDetailItem roomDetail)
            {
                // Close the current details popup
                IsDetailsOpen = false;
                
                // Navigate to rooms and pass the selected room info
                EventBus.Instance.PublishNavigateToRoom(roomDetail.Room.Id);
            }
        }

        private ObservableCollection<RoomDetailItem> _detailRooms;
        public ObservableCollection<RoomDetailItem> DetailRooms
        {
            get => _detailRooms;
            set => SetProperty(ref _detailRooms, value);
        }

        private void ApplyTimeFilter()
        {
            switch (SelectedTimeFilter)
            {
                case "All Time":
                    FilterStartDate = null;
                    FilterEndDate = null;
                    break;
                case "Last 7 Days":
                    FilterStartDate = DateTime.Today.AddDays(-7);
                    FilterEndDate = DateTime.Today;
                    break;
                case "Last 30 Days":
                    FilterStartDate = DateTime.Today.AddDays(-30);
                    FilterEndDate = DateTime.Today;
                    break;
                case "This Month":
                    FilterStartDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
                    FilterEndDate = DateTime.Today;
                    break;
                case "Last Month":
                    var lastMonth = DateTime.Today.AddMonths(-1);
                    FilterStartDate = new DateTime(lastMonth.Year, lastMonth.Month, 1);
                    FilterEndDate = new DateTime(lastMonth.Year, lastMonth.Month, DateTime.DaysInMonth(lastMonth.Year, lastMonth.Month));
                    break;
                case "This Year":
                    FilterStartDate = new DateTime(DateTime.Today.Year, 1, 1);
                    FilterEndDate = DateTime.Today;
                    break;
                case "Custom Range":
                    // Keep existing custom dates
                    break;
            }
        }

        private void RefreshDetailView()
        {
            IEnumerable<ReservationDisplayModel> query;

            switch (DetailType)
            {
                case "Occupancy":
                    query = DataStore.Data.Reservations
                        .Select(r => new ReservationDisplayModel
                        {
                            BaseReservation = r,
                            CustomerName = DataStore.Data.Customers.FirstOrDefault(c => c.Id == r.CustomerId)?.FullName ?? "Unknown Guest",
                            RoomNumber = DataStore.Data.Rooms.FirstOrDefault(room => room.Id == r.RoomId)?.RoomNumber ?? "Unassigned"
                        });
                    break;
                case "ADR":
                    var currentMonth = DateTime.Today.Month;
                    query = DataStore.Data.Reservations
                        .Where(r => r.CheckIn.Month == currentMonth && (r.RoomStatus == "CheckedIn" || r.RoomStatus == "CheckedOut"))
                        .Select(r => new ReservationDisplayModel
                        {
                            BaseReservation = r,
                            CustomerName = DataStore.Data.Customers.FirstOrDefault(c => c.Id == r.CustomerId)?.FullName ?? "Unknown Guest",
                            RoomNumber = DataStore.Data.Rooms.FirstOrDefault(room => room.Id == r.RoomId)?.RoomNumber ?? "Unassigned"
                        });
                    break;
                case "RevPAR":
                    query = DataStore.Data.Reservations
                        .Where(r => r.RoomStatus == "CheckedIn" || r.RoomStatus == "CheckedOut")
                        .Select(r => new ReservationDisplayModel
                        {
                            BaseReservation = r,
                            CustomerName = DataStore.Data.Customers.FirstOrDefault(c => c.Id == r.CustomerId)?.FullName ?? "Unknown Guest",
                            RoomNumber = DataStore.Data.Rooms.FirstOrDefault(room => room.Id == r.RoomId)?.RoomNumber ?? "Unassigned"
                        });
                    break;
                case "Arrivals":
                    query = DataStore.Data.Reservations
                        .Where(r => r.CheckIn.Date == DateTime.Today)
                        .Select(r => new ReservationDisplayModel
                        {
                            BaseReservation = r,
                            CustomerName = DataStore.Data.Customers.FirstOrDefault(c => c.Id == r.CustomerId)?.FullName ?? "Unknown Guest",
                            RoomNumber = DataStore.Data.Rooms.FirstOrDefault(room => room.Id == r.RoomId)?.RoomNumber ?? "Unassigned"
                        });
                    break;
                default:
                    query = new ObservableCollection<ReservationDisplayModel>();
                    break;
            }

            if (DetailFilter != "All")
                query = query.Where(r => r.BaseReservation.RoomStatus == DetailFilter || r.BaseReservation.PaymentStatus == DetailFilter);

            // Apply date range filter
            if (FilterStartDate.HasValue)
                query = query.Where(r => r.CheckInDate >= FilterStartDate.Value);
            if (FilterEndDate.HasValue)
                query = query.Where(r => r.CheckInDate <= FilterEndDate.Value);

            DetailItems = new ObservableCollection<ReservationDisplayModel>(query.OrderByDescending(r => r.CheckInDate));
        }

        public class RoomDetailItem
        {
            public RoomModel Room { get; set; }
            public string RoomType { get; set; }
            public decimal BasePrice { get; set; }
            public string CurrentGuest { get; set; }
            public ReservationModel CurrentReservation { get; set; }
            public string DisplayStatus { get; set; }
            public bool IsOccupied => DisplayStatus == "Occupied";
            public bool IsAvailable => DisplayStatus == "Available";
            public bool IsMaintenance => DisplayStatus == "Maintenance";
        }

        private void LoadData()
        {
            var query = DataStore.Data.Reservations.Select(r => new ReservationDisplayModel
            {
                BaseReservation = r,
                CustomerName = DataStore.Data.Customers.FirstOrDefault(c => c.Id == r.CustomerId)?.FullName ?? "Unknown Guest",
                RoomNumber = DataStore.Data.Rooms.FirstOrDefault(room => room.Id == r.RoomId)?.RoomNumber ?? "Unassigned"
            });

            if (FilterStatus != "All")
                query = query.Where(r => r.BaseReservation.RoomStatus == FilterStatus || r.BaseReservation.PaymentStatus == FilterStatus);

            RecentReservations = new ObservableCollection<ReservationDisplayModel>(query.OrderByDescending(r => r.CheckInDate).Take(5));

            // Start with empty feed for fresh system
            FrontDeskFeed = new ObservableCollection<FeedItem>();

            // Notify UI of header changes
            OnPropertyChanged(nameof(OccupancyText));
            OnPropertyChanged(nameof(OccupancyTrendText));
            OnPropertyChanged(nameof(OccupancyTrendBrush));
            OnPropertyChanged(nameof(AdrText));
            OnPropertyChanged(nameof(AdrTrendText));
            OnPropertyChanged(nameof(AdrTrendBrush));
            OnPropertyChanged(nameof(RevparText));
            OnPropertyChanged(nameof(RevparTrendText));
            OnPropertyChanged(nameof(RevparTrendBrush));
            OnPropertyChanged(nameof(ArrivalsText));
            OnPropertyChanged(nameof(ActiveCheckinsText));
            OnPropertyChanged(nameof(TotalRooms));
            OnPropertyChanged(nameof(OccupiedRooms));
            OnPropertyChanged(nameof(AvailableRooms));
            OnPropertyChanged(nameof(MaintenanceRooms));
            
            OnPropertyChanged(nameof(OccupiedDashArray));
            OnPropertyChanged(nameof(OccupiedDashOffset));
            OnPropertyChanged(nameof(AvailableDashArray));
            OnPropertyChanged(nameof(AvailableDashOffset));
            OnPropertyChanged(nameof(MaintenanceDashArray));
            OnPropertyChanged(nameof(MaintenanceDashOffset));
        }
    }
}
