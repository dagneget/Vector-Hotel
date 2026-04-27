using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using HRS.Models;
using HRS.Services;
using System.Windows;
using System.Windows.Data;
using System.Threading.Tasks;

namespace HRS.ViewModels
{
    public class ReservationDisplayModel : ViewModelBase
    {
        public ReservationModel BaseReservation { get; set; }
        
        public string CustomerName { get; set; }
        public string RoomNumber { get; set; }
        public DateTime CheckInDate => BaseReservation.CheckIn;
        public DateTime CheckOutDate => BaseReservation.CheckOut;
        
        public string ReservationStatus
        {
            get => BaseReservation.RoomStatus ?? "None";
            set
            {
                if (BaseReservation.RoomStatus != value && value != null)
                {
                    if (value == "CheckedOut" && PaymentStatus == "Pending")
                    {
                        MessageBox.Show("Cannot check out a guest with a Pending payment status. Please settle the folio first.", "Payment Required", MessageBoxButton.OK, MessageBoxImage.Warning);
                        OnPropertyChanged(nameof(ReservationStatus));
                        return;
                    }
                    BaseReservation.RoomStatus = value;
                    OnPropertyChanged(nameof(ReservationStatus));
                    _ = UpdateStatusAsync(BaseReservation.Id, value);
                }
            }
        }

        private async Task UpdateStatusAsync(string id, string status)
        {
            try 
            { 
                await DataStore.UpdateReservationStatusAsync(id, status); 
            }
            catch (Exception ex) 
            { 
                System.Windows.MessageBox.Show($"Error updating status: {ex.Message}"); 
            }
        }

        public string PaymentStatus => BaseReservation.PaymentStatus ?? "Pending";
        public decimal TotalPrice => BaseReservation != null ? BaseReservation.TotalPrice : 0;
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
                    .Where(r => r.CheckIn.Date == DateTime.Today || r.RoomStatus == "CheckedIn")
                    .Sum(r => r.TotalPrice);
                if (revenue >= 1000) return $"${(revenue / 1000m):F1}k";
                return $"${revenue:F0}";
            }
        }
        public string DailyRevTrend => "";
        public string PendingCount => DataStore.Data.Reservations.Count(r => r.PaymentStatus == "Pending").ToString();
        
        public string SummaryText 
        {
            get {
                int arriving = DataStore.Data.Reservations.Count(r => r.CheckIn.Date == DateTime.Today);
                int pending = DataStore.Data.Reservations.Count(r => r.PaymentStatus == "Pending");
                return $"{arriving} GUESTS ARRIVING TODAY • {pending} PENDING ACTIONS";
            }
        }

        public string SystemStatus => "Heartbeat: Stable";
        public string LastUpdatedText => $"LAST UPDATED: {DateTime.Now:HH:mm}";
        
        public bool CanManageReservations => AuthService.IsAdmin() || AuthService.IsReceptionist();
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
                    if (value == null)
                    {
                        IsViewingDetails = false;
                        IsEditing = false;
                    }

                    OnPropertyChanged(nameof(QuickChangeRoomStatus));

                    if (value != null)
                    {
                        try
                        {
                            PopulateForm(value);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Error loading reservation: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }
            }
        }

        private bool _isEditing;
        public bool IsEditing
        {
            get => _isEditing;
            set
            {
                if (SetProperty(ref _isEditing, value))
                    OnPropertyChanged(nameof(IsPanelOpen));
            }
        }

        private bool _isViewingDetails;
        public bool IsViewingDetails
        {
            get => _isViewingDetails;
            set
            {
                if (SetProperty(ref _isViewingDetails, value))
                    OnPropertyChanged(nameof(IsPanelOpen));
            }
        }

        public string QuickChangeRoomStatus
        {
            get => SelectedReservation?.ReservationStatus;
            set
            {
                if (SelectedReservation != null && SelectedReservation.ReservationStatus != value && value != null)
                {
                    if (value == "CheckedOut" && SelectedReservation.PaymentStatus == "Pending")
                    {
                        MessageBox.Show("Cannot check out a guest with a Pending payment status. Please settle the folio first.", "Payment Required", MessageBoxButton.OK, MessageBoxImage.Warning);
                        OnPropertyChanged(nameof(QuickChangeRoomStatus));
                        return;
                    }
                    SelectedReservation.BaseReservation.RoomStatus = value;
                    _ = UpdateQuickStatusAsync(SelectedReservation.BaseReservation.Id, value);
                    OnPropertyChanged(nameof(QuickChangeRoomStatus));
                }
            }
        }

        private async Task UpdateQuickStatusAsync(string id, string newStatus)
        {
            try
            {
                await DataStore.UpdateReservationStatusAsync(id, newStatus);
                AuditService.Log("Quick Status Update", $"Reservation {id} status changed to {newStatus}.", "Modification", "Info");
                LoadData();
                SelectedReservation = Reservations.FirstOrDefault(r => r.BaseReservation.Id == id);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error updating status: {ex.Message}");
            }
        }

        public bool IsPanelOpen => IsEditing || IsViewingDetails;

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
        public RoomModel FormSelectedRoom 
        { 
            get => _formSelectedRoom; 
            set 
            { 
                if (SetProperty(ref _formSelectedRoom, value))
                {
                    // Reset extra bed when room changes
                    OnPropertyChanged(nameof(FormSelectedRoomHasExtraBed));
                    if (value == null || !value.HasExtraBed)
                        FormWantsExtraBed = false;
                    CalculatePrice();
                } 
            } 
        }

        /// <summary>True when the selected room offers an extra bed option.</summary>
        public bool FormSelectedRoomHasExtraBed => FormSelectedRoom?.HasExtraBed == true && FormSelectedRoom.ExtraBedPrice > 0;

        private bool _formWantsExtraBed;
        public bool FormWantsExtraBed
        {
            get => _formWantsExtraBed;
            set
            {
                if (SetProperty(ref _formWantsExtraBed, value))
                    CalculatePrice();
            }
        }

        private DateTime _formCheckInDate;
        public DateTime FormCheckInDate
        {
            get => _formCheckInDate;
            set
            {
                if (SetProperty(ref _formCheckInDate, value)) 
                { 
                    OnPropertyChanged(nameof(FormCheckInDatePlusOne));
                    UpdateAvailableRooms(); 
                    CalculatePrice(); 
                }
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

        private string _formPaymentStatus;
        public string FormPaymentStatus { get => _formPaymentStatus; set => SetProperty(ref _formPaymentStatus, value); }

        private string _formRoomStatus;
        public string FormRoomStatus 
        { 
            get => _formRoomStatus; 
            set 
            { 
                if (SetProperty(ref _formRoomStatus, value))
                {
                    if (value == "CheckedOut" && FormPaymentStatus == "Pending")
                    {
                        MessageBox.Show("Cannot check out a guest with a Pending payment status. Please settle the folio first.", "Payment Required", MessageBoxButton.OK, MessageBoxImage.Warning);
                        _formRoomStatus = "CheckedIn"; // Revert to safe state
                        OnPropertyChanged(nameof(FormRoomStatus));
                        return;
                    }
                    if (value == "CheckedOut" && FormCheckOutDate > DateTime.Today && FormCheckInDate <= DateTime.Today)
                    {
                        FormCheckOutDate = DateTime.Today == FormCheckInDate ? DateTime.Today.AddDays(1) : DateTime.Today;
                    }
                    else if (value == "Cancelled")
                    {
                        FormTotalPrice = 0;
                    }
                }
            } 
        }

        public string[] PaymentStatusOptions => new[] { "Pending", "Confirmed" };
        public string[] RoomStatusOptions => new[] { "None", "CheckedIn", "CheckedOut", "Cancelled" };
        public string[] PricingPlanOptions => new[] { "Base", "Weekend", "Holiday" };

        private string _formPricingPlan;
        public string FormPricingPlan
        {
            get => _formPricingPlan;
            set
            {
                if (SetProperty(ref _formPricingPlan, value))
                {
                    CalculatePrice();
                }
            }
        }

        public DateTime TodayDate => DateTime.Today;
        public DateTime FormCheckInDatePlusOne => FormCheckInDate.Date >= DateTime.Today ? FormCheckInDate.Date.AddDays(1) : DateTime.Today.AddDays(1);

        public ICommand NewReservationCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand CancelEditCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand ViewReservationCommand { get; }
        public ICommand EditReservationCommand { get; }

        public ReservationsViewModel()
        {
            NewReservationCommand = new RelayCommand(_ => { if (CanManageReservations) CreateNew(); });
            SaveCommand = new RelayCommand(_ => { if (CanManageReservations) Save(); });
            CancelEditCommand = new RelayCommand(_ => CancelEdit());
            DeleteCommand = new RelayCommand(_ => { if (CanManageReservations) DeleteSelected(); });
            ChangeSegmentCommand = new RelayCommand(p => CurrentSegment = p as string);
            ViewReservationCommand = new RelayCommand(r => { SelectedReservation = r as ReservationDisplayModel; IsViewingDetails = true; IsEditing = false; });
            EditReservationCommand = new RelayCommand(r => { if (CanManageReservations) { SelectedReservation = r as ReservationDisplayModel; IsEditing = true; IsViewingDetails = false; } });

            CustomersList = new ObservableCollection<CustomerModel>(DataStore.Data.Customers.OrderBy(c => c.FullName));
            RoomTypesList = new ObservableCollection<RoomTypeModel>(DataStore.Data.RoomTypes);

            LoadData();
        }

        private void CancelEdit()
        {
            SelectedReservation = null;
            IsEditing = false;
            IsViewingDetails = false;
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
                    r.PaymentStatus.ToLower().Contains(s) ||
                    r.BaseReservation.Id.Contains(s));
            }

            if (CurrentSegment == "Arrivals")
                query = query.Where(r => r.BaseReservation.CheckIn.Date == DateTime.Today);
            else if (CurrentSegment == "InHouse")
                query = query.Where(r => r.BaseReservation.RoomStatus == "CheckedIn");
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
            if (FormCheckInDate >= FormCheckOutDate)
            {
                FormTotalPrice = 0;
                return;
            }

            int days = (int)(FormCheckOutDate.Date - FormCheckInDate.Date).TotalDays;
            if (days <= 0) days = 1;

            decimal total = 0;

            if (FormSelectedRoom != null)
            {
                decimal ratePerNight = FormSelectedRoom.BasePricePerNight;
                if (FormPricingPlan == "Weekend") ratePerNight = FormSelectedRoom.WeekendPrice;
                else if (FormPricingPlan == "Holiday") ratePerNight = FormSelectedRoom.HolidayPrice;

                total = ratePerNight * days;

                // Add extra bed cost per night if the customer requested it
                if (FormWantsExtraBed && FormSelectedRoom.HasExtraBed)
                    total += FormSelectedRoom.ExtraBedPrice * days;
            }
            else if (FormSelectedRoomType != null)
            {
                total = FormSelectedRoomType.BasePrice * days;
            }

            FormTotalPrice = total;
        }

        public void TriggerNewReservation()
        {
            CreateNew();
        }

        private void CreateNew()
        {
            SelectedReservation = null;
            FormSelectedCustomer = null;
            GuestSearchText = "";
            FormSelectedRoomType = null;
            FormSelectedRoom = null;
            FormCheckInDate = DateTime.Today;
            FormCheckOutDate = DateTime.Today.AddDays(1);
            FormAdultsCount = 1;
            FormWantsExtraBed = false;
            FormPricingPlan = "Base";
            FormTotalPrice = 0;
            FormNotes = "";
            FormPaymentStatus = "Pending";
            FormRoomStatus = "None";

            UpdateAvailableRooms();
            IsViewingDetails = false;
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
            FormPaymentStatus = r.PaymentStatus ?? "Pending";
            FormRoomStatus = r.RoomStatus ?? "None";
            FormPricingPlan = r.PricingPlan ?? "Base";
            FormWantsExtraBed = r.WantsExtraBed;
        }

        private async void Save()
        {
            if (!IsValid)
            {
                var errors = string.Join("\n", AllErrors);
                MessageBox.Show($"Please fix the following errors:\n\n{errors}", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
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
            r.PaymentStatus = FormPaymentStatus;
            r.RoomStatus = FormRoomStatus == "None" ? null : FormRoomStatus;
            r.PricingPlan = FormPricingPlan;
            r.WantsExtraBed = FormWantsExtraBed;
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

        // --- Validation Logic ---
        protected override void ValidateProperty(string propertyName)
        {
            RemoveError(propertyName);

            switch (propertyName)
            {
                case nameof(FormCheckInDate):
                    if (FormCheckInDate.Date < DateTime.Today)
                        AddError(propertyName, "Check-in date cannot be in the past.");
                    break;

                case nameof(FormCheckOutDate):
                    if (FormCheckOutDate.Date <= FormCheckInDate.Date)
                        AddError(propertyName, "Check-out must be after check-in.");
                    break;

                case nameof(FormSelectedCustomer):
                    if (FormSelectedCustomer == null)
                        AddError(propertyName, "A customer must be selected.");
                    else if (FormSelectedCustomer.IsBlacklisted)
                        AddError(propertyName, $"Guest {FormSelectedCustomer.FullName} is BLACKLISTED: {FormSelectedCustomer.BlacklistReason}");
                    break;

                case nameof(FormSelectedRoom):
                    if (FormSelectedRoom == null)
                        AddError(propertyName, "A room must be selected.");
                    break;

                case nameof(FormAdultsCount):
                    if (FormAdultsCount <= 0)
                        AddError(propertyName, "Adults count must be at least 1.");
                    else if (FormSelectedRoom != null && FormAdultsCount > FormSelectedRoom.MaxOccupancy)
                        AddError(propertyName, $"Selected room capacity is {FormSelectedRoom.MaxOccupancy} persons.");
                    break;
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
                        string resId = SelectedReservation.BaseReservation.Id;
                        string guestName = SelectedReservation.CustomerName;
                        await ApiService.DeleteAsync($"reservations/{resId}");
                        AuditService.Log("Reservation Deleted", $"Deleted reservation {resId} for {guestName}.", "Modification", "Warning");
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
