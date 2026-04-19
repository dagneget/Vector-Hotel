using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using HRS.Models;
using HRS.Services;
using System.Windows;
using System.Windows.Data;

namespace HRS.ViewModels
{
    public class ReservationDisplayModel 
    {
        public ReservationModel BaseReservation { get; set; }
        
        public string CustomerName { get; set; }
        public string RoomNumber { get; set; }
        public DateTime CheckInDate => BaseReservation.CheckIn;
        public DateTime CheckOutDate => BaseReservation.CheckOut;
        public string ReservationStatus => BaseReservation.Status;
        public decimal TotalPrice => BaseReservation.TotalPrice;
        
        // Mock payment status based on whether it is confirmed
        public string PaymentStatus => BaseReservation.Status == "CheckedIn" || BaseReservation.Status == "CheckedOut" ? "Paid" : (BaseReservation.Status == "Confirmed" ? "Paid" : "Pending");
    }

    public class ReservationsViewModel : ViewModelBase
    {
        private ObservableCollection<ReservationDisplayModel> _reservations;
        public ObservableCollection<ReservationDisplayModel> Reservations
        {
            get => _reservations;
            set => SetProperty(ref _reservations, value);
        }

        // --- Dashboard Metrics (Live Data) --- 
        public string OccupancyText 
        {
            get {
                var total = DataStore.Data.Rooms.Count();
                if (total == 0) return "0%";
                var occupied = DataStore.Data.Rooms.Count(r => r.Status == "Occupied" || r.Status == "Reserved");
                return $"{(occupied * 100.0 / total):F0}%";
            }
        }
        public string OccupancyTrend => ""; // Keep empty or calculate from history if available
        public string DailyRevText 
        {
            get {
                var revenue = DataStore.Data.Reservations
                    .Where(r => r.CheckIn.Date == DateTime.Today || (r.Status == "CheckedIn"))
                    .Sum(r => r.TotalPrice);
                if (revenue >= 1000) return $"${(revenue / 1000m):F1}k";
                return $"${revenue:F0}";
            }
        }
        public string DailyRevTrend => "";
        public string PendingCount => DataStore.Data.Reservations.Count(r => r.Status == "Pending").ToString();
        
        public string SummaryText 
        {
            get {
                int arriving = DataStore.Data.Reservations.Count(r => r.CheckIn.Date == DateTime.Today);
                int pending = DataStore.Data.Reservations.Count(r => r.Status == "Pending");
                return $"{arriving} GUESTS ARRIVING TODAY • {pending} PENDING ACTIONS";
            }
        }

        public string SystemStatus => "Heartbeat: Stable";
        public string LastUpdatedText => $"LAST UPDATED: {DateTime.Now:HH:mm}";
        // -------------------------

        private string _searchText;
        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value)) FilterData();
            }
        }

        private string _currentSegment = "All";
        public string CurrentSegment
        {
            get => _currentSegment;
            set { if (SetProperty(ref _currentSegment, value)) FilterData(); }
        }

        public ICommand ChangeSegmentCommand { get; }

        // --- Editing Properties ---
        private ReservationDisplayModel _selectedReservation;
        public ReservationDisplayModel SelectedReservation
        {
            get => _selectedReservation;
            set
            {
                if (SetProperty(ref _selectedReservation, value))
                {
                    IsEditing = value != null;
                    if (value != null) PopulateForm(value);
                }
            }
        }

        private bool _isEditing;
        public bool IsEditing
        {
            get => _isEditing;
            set => SetProperty(ref _isEditing, value);
        }

        // --- Form Properties ---
        public ObservableCollection<CustomerModel> CustomersList { get; set; }
        public ObservableCollection<RoomTypeModel> RoomTypesList { get; set; }

        private ObservableCollection<RoomModel> _availableRoomsList;
        public ObservableCollection<RoomModel> AvailableRoomsList
        {
            get => _availableRoomsList;
            set => SetProperty(ref _availableRoomsList, value);
        }

        private CustomerModel _formSelectedCustomer;
        public CustomerModel FormSelectedCustomer { get => _formSelectedCustomer; set => SetProperty(ref _formSelectedCustomer, value); }
        
        private string _guestSearchText;
        public string GuestSearchText 
        { 
            get => _guestSearchText; 
            set 
            { 
                if (SetProperty(ref _guestSearchText, value))
                {
                    FilterCustomers();
                }
            } 
        }

        private void FilterCustomers()
        {
            if (CustomersList == null) return;
            var view = CollectionViewSource.GetDefaultView(CustomersList);
            if (string.IsNullOrWhiteSpace(GuestSearchText) || FormSelectedCustomer?.FullName == GuestSearchText)
            {
                view.Filter = null;
            }
            else
            {
                view.Filter = item =>
                {
                    if (item is CustomerModel c)
                    {
                        return c.FullName != null && c.FullName.IndexOf(GuestSearchText, StringComparison.OrdinalIgnoreCase) >= 0;
                    }
                    return false;
                };
            }
        }

        private RoomTypeModel _formSelectedRoomType;
        public RoomTypeModel FormSelectedRoomType
        {
            get => _formSelectedRoomType;
            set
            {
                if (SetProperty(ref _formSelectedRoomType, value))
                {
                    UpdateAvailableRooms();
                    CalculatePrice();
                }
            }
        }

        private RoomModel _formSelectedRoom;
        public RoomModel FormSelectedRoom { get => _formSelectedRoom; set => SetProperty(ref _formSelectedRoom, value); }

        private DateTime _formCheckInDate;
        public DateTime FormCheckInDate
        {
            get => _formCheckInDate;
            set
            {
                if (SetProperty(ref _formCheckInDate, value)) { UpdateAvailableRooms(); CalculatePrice(); }
            }
        }

        private DateTime _formCheckOutDate;
        public DateTime FormCheckOutDate
        {
            get => _formCheckOutDate;
            set
            {
                if (SetProperty(ref _formCheckOutDate, value)) { UpdateAvailableRooms(); CalculatePrice(); }
            }
        }

        private int _formAdultsCount;
        public int FormAdultsCount { get => _formAdultsCount; set => SetProperty(ref _formAdultsCount, value); }

        private decimal _formTotalPrice;
        public decimal FormTotalPrice { get => _formTotalPrice; set => SetProperty(ref _formTotalPrice, value); }

        private string _formNotes;
        public string FormNotes { get => _formNotes; set => SetProperty(ref _formNotes, value); }

        private string _formStatus;
        public string FormStatus { get => _formStatus; set => SetProperty(ref _formStatus, value); }

        public string[] StatusOptions => new[] { "Pending", "Confirmed", "CheckedIn", "CheckedOut", "Cancelled" };

        public ICommand NewReservationCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand CancelEditCommand { get; }
        public ICommand DeleteCommand { get; }

        public ReservationsViewModel()
        {
            NewReservationCommand = new RelayCommand(_ => CreateNew());
            SaveCommand = new RelayCommand(_ => Save());
            CancelEditCommand = new RelayCommand(_ => CancelEdit());
            DeleteCommand = new RelayCommand(_ => DeleteSelected());
            ChangeSegmentCommand = new RelayCommand(p => CurrentSegment = p as string);

            CustomersList = new ObservableCollection<CustomerModel>(DataStore.Data.Customers.OrderBy(c => c.FullName));
            RoomTypesList = new ObservableCollection<RoomTypeModel>(DataStore.Data.RoomTypes);

            LoadData();
        }

        private void CancelEdit()
        {
            SelectedReservation = null;
            IsEditing = false;
        }

        private void LoadData()
        {
            FilterData();
        }

        private void FilterData()
        {
            var query = DataStore.Data.Reservations.Select(r => new ReservationDisplayModel
            {
                BaseReservation = r,
                CustomerName = DataStore.Data.Customers.FirstOrDefault(c => c.Id == r.CustomerId)?.FullName ?? "Unknown Guest",
                RoomNumber = DataStore.Data.Rooms.FirstOrDefault(room => room.Id == r.RoomId)?.RoomNumber ?? "Unassigned"
            });

            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var s = SearchText.ToLower();
                query = query.Where(r => 
                    r.CustomerName.ToLower().Contains(s) ||
                    r.RoomNumber.ToLower().Contains(s) ||
                    r.ReservationStatus.ToLower().Contains(s) ||
                    r.BaseReservation.Id.Contains(s));
            }

            if (CurrentSegment == "Arrivals")
                query = query.Where(r => r.BaseReservation.CheckIn.Date == DateTime.Today);
            else if (CurrentSegment == "InHouse")
                query = query.Where(r => r.BaseReservation.Status == "CheckedIn");
            else if (CurrentSegment == "Departures")
                query = query.Where(r => r.BaseReservation.CheckOut.Date == DateTime.Today);

            Reservations = new ObservableCollection<ReservationDisplayModel>(query.OrderByDescending(r => r.CheckInDate));
        }

        private void UpdateAvailableRooms()
        {
            if (FormCheckInDate >= FormCheckOutDate || FormSelectedRoomType == null)
            {
                AvailableRoomsList = new ObservableCollection<RoomModel>();
                return;
            }

            var excludeId = SelectedReservation?.BaseReservation.Id;
            var availableRooms = ReservationService.GetAvailableRooms(FormCheckInDate, FormCheckOutDate)
                .Where(r => r.TypeId == FormSelectedRoomType.Id)
                .ToList();

            // Always add the currently assigned room so it doesn't disappear from the dropdown when editing
            if (SelectedReservation != null)
            {
                var currentRoom = DataStore.Data.Rooms.FirstOrDefault(r => r.Id == SelectedReservation.BaseReservation.RoomId);
                if (currentRoom != null && currentRoom.TypeId == FormSelectedRoomType.Id && !availableRooms.Any(r => r.Id == currentRoom.Id))
                {
                    availableRooms.Insert(0, currentRoom);
                }
            }

            AvailableRoomsList = new ObservableCollection<RoomModel>(availableRooms);

            // Reselect current room if it exists in the list
            if (FormSelectedRoom != null) 
            {
                var match = AvailableRoomsList.FirstOrDefault(r => r.Id == FormSelectedRoom.Id);
                FormSelectedRoom = match;
            }
            if (FormSelectedRoom == null && AvailableRoomsList.Count > 0)
            {
                FormSelectedRoom = AvailableRoomsList.FirstOrDefault();
            }
        }

        private void CalculatePrice()
        {
            if (FormSelectedRoomType == null || FormCheckInDate >= FormCheckOutDate)
            {
                FormTotalPrice = 0;
                return;
            }

            int days = (int)(FormCheckOutDate.Date - FormCheckInDate.Date).TotalDays;
            if (days <= 0) days = 1;
            FormTotalPrice = FormSelectedRoomType.BasePrice * days;
        }

        private void CreateNew()
        {
            SelectedReservation = null;
            FormCheckInDate = DateTime.Today;
            FormCheckOutDate = DateTime.Today.AddDays(1);
            FormSelectedCustomer = CustomersList.FirstOrDefault();
            
            FormSelectedRoomType = null;
            FormSelectedRoom = null;
            AvailableRoomsList = new ObservableCollection<RoomModel>();
            
            FormAdultsCount = 1;
            FormNotes = "";
            FormStatus = "Pending";
            FormTotalPrice = 0;
            IsEditing = true;
        }

        private void PopulateForm(ReservationDisplayModel display)
        {
            var r = display.BaseReservation;
            FormCheckInDate = r.CheckIn;
            FormCheckOutDate = r.CheckOut;
            FormSelectedCustomer = CustomersList.FirstOrDefault(c => c.Id == r.CustomerId);
            
            var currentRoom = DataStore.Data.Rooms.FirstOrDefault(rm => rm.Id == r.RoomId);
            if (currentRoom != null)
            {
                FormSelectedRoomType = RoomTypesList.FirstOrDefault(t => t.Id == currentRoom.TypeId);
            }
            
            UpdateAvailableRooms();
            FormSelectedRoom = AvailableRoomsList.FirstOrDefault(rm => rm.Id == r.RoomId);
            
            FormAdultsCount = r.AdultsCount;
            FormTotalPrice = r.TotalPrice;
            FormNotes = r.Notes ?? "";
            FormStatus = r.Status;
        }

        private async void Save()
        {
            if (FormSelectedCustomer == null || FormSelectedRoom == null || FormCheckInDate >= FormCheckOutDate)
            {
                MessageBox.Show("Please complete all required fields correctly. Ensure checkout is after check-in.");
                return;
            }

            string resId = SelectedReservation?.BaseReservation.Id ?? DataStore.GenerateId();

            if (!ReservationService.IsRoomAvailable(FormSelectedRoom.Id, FormCheckInDate, FormCheckOutDate, resId))
            {
                MessageBox.Show("The selected room is not available for these dates.", "Conflict Detected", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var r = (SelectedReservation == null) ? new ReservationModel() : SelectedReservation.BaseReservation;
            r.CustomerId = FormSelectedCustomer.Id;
            r.RoomId = FormSelectedRoom.Id;
            r.CheckIn = FormCheckInDate;
            r.CheckOut = FormCheckOutDate;
            r.AdultsCount = FormAdultsCount;
            r.TotalPrice = FormTotalPrice;
            r.Notes = FormNotes;
            r.Status = FormStatus;
            r.LastModified = DateTime.Now;

            try 
            {
                if (SelectedReservation == null)
                {
                    r.Id = resId;
                    r.Source = "Manual";
                    await ApiService.PostAsync<ReservationModel>("reservations", r);
                    AuditService.Log("Reservation Created", $"Created new reservation for {FormSelectedCustomer.FullName}.");
                }
                else
                {
                    await ApiService.PutAsync($"reservations/{r.Id}", r);
                    AuditService.Log("Reservation Updated", $"Updated reservation {r.Id} for {FormSelectedCustomer.FullName}.");
                }

                await DataStore.LoadAsync();
                IsEditing = false;
                LoadData();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving reservation: {ex.Message}");
            }
        }

        private async void DeleteSelected()
        {
            if (SelectedReservation != null)
            {
                var res = MessageBox.Show($"Are you sure you want to delete this reservation?", "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (res == MessageBoxResult.Yes)
                {
                    try 
                    {
                        await ApiService.DeleteAsync($"reservations/{SelectedReservation.BaseReservation.Id}");
                        await DataStore.LoadAsync();
                        IsEditing = false;
                        LoadData();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error deleting reservation: {ex.Message}");
                    }
                }
            }
        }
    }
}
