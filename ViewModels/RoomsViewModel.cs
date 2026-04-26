using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using HRS.Models;
using HRS.Services;

namespace HRS.ViewModels
{
    public class RoomDisplayModel : ViewModelBase
    {
        public RoomModel BaseRoom { get; set; }
        
        // Basic Information
        public string RoomNumber => BaseRoom?.RoomNumber;
        public int FloorNumber => BaseRoom?.FloorNumber ?? 0;
        public string CleanStatus => BaseRoom?.CleanStatus;
        public string Status => BaseRoom?.Status;
        public decimal RoomSize => BaseRoom?.RoomSize ?? 0;
        public string Description => BaseRoom?.Description;
        
        // Status
        public string AvailabilityStatus => BaseRoom?.AvailabilityStatus ?? "Available";
        public string OperationalStatus => BaseRoom?.OperationalStatus ?? "Normal";
        
        // Capacity & Bed Configuration
        public int MaxOccupancy => BaseRoom?.MaxOccupancy ?? 2;
        public int NumberOfBeds => BaseRoom?.NumberOfBeds ?? 1;
        public string BedType => BaseRoom?.BedType ?? "Queen";
        public bool HasExtraBed => BaseRoom?.HasExtraBed ?? false;
        public decimal ExtraBedPrice => BaseRoom?.ExtraBedPrice ?? 0;
        
        // Pricing
        public decimal BasePricePerNight => BaseRoom?.BasePricePerNight ?? 0;
        public decimal WeekendPrice => BaseRoom?.WeekendPrice ?? 0;
        public decimal HolidayPrice => BaseRoom?.HolidayPrice ?? 0;
        public string Currency => BaseRoom?.Currency ?? "USD";
        
        // Joined Data from RoomType
        public string CategoryName { get; set; }
        public decimal BasePrice { get; set; }
        
        // Amenities
        public List<string> Amenities => BaseRoom?.Amenities ?? new List<string>();
        public bool HasAmenity(string amenity) => Amenities?.Contains(amenity) ?? false;
        
        // Images
        public List<string> ImageUrls => BaseRoom?.ImageUrls ?? new List<string>();
        public string MainImageUrl => BaseRoom?.MainImageUrl;
        public bool HasImages => ImageUrls?.Count > 0;
        
        // Additional Attributes
        public bool SmokingAllowed => BaseRoom?.SmokingAllowed ?? false;
        public bool WheelchairAccessible => BaseRoom?.WheelchairAccessible ?? false;
        public bool PetFriendly => BaseRoom?.PetFriendly ?? false;
        
        // Housekeeping
        public DateTime? LastCleanedDate => BaseRoom?.LastCleanedDate;
        public string HousekeepingNotes => BaseRoom?.HousekeepingNotes;
        
        // Maintenance
        public string MaintenanceIssue => BaseRoom?.MaintenanceIssue;
        public DateTime? MaintenanceDate => BaseRoom?.MaintenanceDate;
        
        // Notes
        public string StaffNotes => BaseRoom?.StaffNotes;
        public string InternalComments => BaseRoom?.InternalComments;
        
        // Timestamps
        public DateTime CreatedAt => BaseRoom?.CreatedAt ?? DateTime.MinValue;
        public DateTime? UpdatedAt => BaseRoom?.UpdatedAt;
    }

    public class RoomsViewModel : ViewModelBase
    {
        private ObservableCollection<RoomDisplayModel> _rooms;
        public ObservableCollection<RoomDisplayModel> Rooms
        {
            get => _rooms;
            set => SetProperty(ref _rooms, value);
        }

        private ObservableCollection<RoomTypeModel> _roomTypes;
        public ObservableCollection<RoomTypeModel> RoomTypes
        {
            get => _roomTypes;
            set => SetProperty(ref _roomTypes, value);
        }

        private string _searchText;
        public string SearchText
        {
            get => _searchText;
            set { if (SetProperty(ref _searchText, value)) FilterData(); }
        }

        // --- Selection & Editing ---
        private RoomDisplayModel _selectedRoom;
        public RoomDisplayModel SelectedRoom
        {
            get => _selectedRoom;
            set
            {
                if (SetProperty(ref _selectedRoom, value))
                {
                    // Don't auto-show side panel anymore - use dialog instead
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

        private bool _isViewingDetails;
        public bool IsViewingDetails
        {
            get => _isViewingDetails;
            set => SetProperty(ref _isViewingDetails, value);
        }

        // Form Fields
        private string _formRoomNumber;
        public string FormRoomNumber { get => _formRoomNumber; set => SetProperty(ref _formRoomNumber, value); }

        private string _formFloor;
        public string FormFloor { get => _formFloor; set => SetProperty(ref _formFloor, value); }

        private RoomTypeModel _formSelectedType;
        public RoomTypeModel FormSelectedType { get => _formSelectedType; set => SetProperty(ref _formSelectedType, value); }

        private string _formCleanStatus;
        public string FormCleanStatus { get => _formCleanStatus; set => SetProperty(ref _formCleanStatus, value); }

        public string[] CleanStatusOptions => new[] { "Clean", "Dirty", "Maintenance" };

        // Commands
        public ICommand SaveCommand { get; }
        public ICommand CancelEditCommand { get; }
        public ICommand RegisterRoomCommand { get; }
        public ICommand DeleteRoomCommand { get; }
        public ICommand EditRoomCommand { get; }
        public ICommand ViewRoomDetailsCommand { get; }
        public ICommand StartEditCommand { get; }

        public RoomsViewModel()
        {
            SaveCommand = new RelayCommand(_ => Save());
            CancelEditCommand = new RelayCommand(_ => { IsEditing = false; IsViewingDetails = false; });
            RegisterRoomCommand = new RelayCommand(_ => PrepareNew());
            DeleteRoomCommand = new RelayCommand(_ => DeleteSelected());
            EditRoomCommand = new RelayCommand(_ => EditExisting());
            ViewRoomDetailsCommand = new RelayCommand(room => ViewRoomDetails(room as RoomDisplayModel));
            StartEditCommand = new RelayCommand(_ => { IsViewingDetails = false; IsEditing = true; });

            RoomTypes = new ObservableCollection<RoomTypeModel>(DataStore.Data.RoomTypes);
            LoadData();
        }

        private void LoadData()
        {
            FilterData();
        }

        private void FilterData()
        {
            var query = DataStore.Data.Rooms.Select(r => new RoomDisplayModel
            {
                BaseRoom = r,
                CategoryName = DataStore.Data.RoomTypes.FirstOrDefault(t => t.Id == r.TypeId)?.Name ?? "Unknown",
                BasePrice = DataStore.Data.RoomTypes.FirstOrDefault(t => t.Id == r.TypeId)?.BasePrice ?? 0
            });

            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                query = query.Where(r => r.RoomNumber.Contains(SearchText));
            }

            Rooms = new ObservableCollection<RoomDisplayModel>(query.OrderBy(r => r.RoomNumber));
        }

        private void PrepareNew()
        {
            var viewModel = new AddRoomViewModel();
            var dialog = new Views.AddRoomDialog
            {
                DataContext = viewModel
            };

            // Set owner to active window, fallback to MainWindow
            var owner = Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive && w != dialog)
                        ?? Application.Current.MainWindow;

            if (owner != null && owner != dialog)
                dialog.Owner = owner;

            var result = dialog.ShowDialog();

            if (result == true && viewModel.IsSuccess)
            {
                // Save the created room via API
                _ = SaveNewRoomAsync(viewModel.CreatedRoom);
            }
        }

        private void EditExisting()
        {
            if (SelectedRoom == null) return;

            // Open the new comprehensive dialog for editing
            var viewModel = new AddRoomViewModel();
            
            // Get room type details
            var roomType = DataStore.Data.RoomTypes.FirstOrDefault(rt => rt.Id == SelectedRoom.BaseRoom.TypeId);
            
            // Create RoomDetailModel from the selected room's data
            var roomDetail = new RoomDetailModel
            {
                Id = SelectedRoom.BaseRoom.Id,
                RoomNumber = SelectedRoom.BaseRoom.RoomNumber,
                FloorNumber = SelectedRoom.BaseRoom.FloorNumber,
                TypeId = SelectedRoom.BaseRoom.TypeId,
                CleanStatus = SelectedRoom.BaseRoom.CleanStatus,
                Status = SelectedRoom.BaseRoom.Status,
                AvailabilityStatus = SelectedRoom.BaseRoom.AvailabilityStatus ?? SelectedRoom.BaseRoom.Status,
                OperationalStatus = SelectedRoom.BaseRoom.OperationalStatus ?? "Normal",
                
                BasePricePerNight = SelectedRoom.BaseRoom.BasePricePerNight > 0 ? SelectedRoom.BaseRoom.BasePricePerNight : (roomType?.BasePrice ?? 0),
                Currency = SelectedRoom.BaseRoom.Currency ?? "USD",
                
                RoomSize = SelectedRoom.BaseRoom.RoomSize,
                MaxOccupancy = SelectedRoom.BaseRoom.MaxOccupancy,
                NumberOfBeds = SelectedRoom.BaseRoom.NumberOfBeds,
                BedType = SelectedRoom.BaseRoom.BedType ?? "Queen",
                HasExtraBed = SelectedRoom.BaseRoom.HasExtraBed,
                ExtraBedPrice = SelectedRoom.BaseRoom.ExtraBedPrice,
                WeekendPrice = SelectedRoom.BaseRoom.WeekendPrice > 0 ? SelectedRoom.BaseRoom.WeekendPrice : (roomType?.BasePrice ?? 0),
                HolidayPrice = SelectedRoom.BaseRoom.HolidayPrice > 0 ? SelectedRoom.BaseRoom.HolidayPrice : (roomType?.BasePrice ?? 0),
                Amenities = SelectedRoom.BaseRoom.Amenities ?? new System.Collections.Generic.List<string>(),
                SmokingAllowed = SelectedRoom.BaseRoom.SmokingAllowed,
                WheelchairAccessible = SelectedRoom.BaseRoom.WheelchairAccessible,
                PetFriendly = SelectedRoom.BaseRoom.PetFriendly,
                ImageUrls = SelectedRoom.BaseRoom.ImageUrls ?? new System.Collections.Generic.List<string>(),
                MainImageUrl = SelectedRoom.BaseRoom.MainImageUrl,
                LastCleanedDate = SelectedRoom.BaseRoom.LastCleanedDate,
                HousekeepingNotes = SelectedRoom.BaseRoom.HousekeepingNotes,
                MaintenanceIssue = SelectedRoom.BaseRoom.MaintenanceIssue,
                MaintenanceDate = SelectedRoom.BaseRoom.MaintenanceDate,
                StaffNotes = SelectedRoom.BaseRoom.StaffNotes,
                InternalComments = SelectedRoom.BaseRoom.InternalComments
            };
            
            // Populate the dialog with existing room data
            viewModel.LoadFromRoomDetail(roomDetail);
            
            var dialog = new Views.AddRoomDialog
            {
                DataContext = viewModel,
                Title = "Edit Room" // Change title to indicate editing
            };

            // Set owner to active window
            var owner = Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive && w != dialog)
                        ?? Application.Current.MainWindow;

            if (owner != null && owner != dialog)
                dialog.Owner = owner;

            var result = dialog.ShowDialog();

            if (result == true && viewModel.IsSuccess)
            {
                // Update existing room via API
                _ = UpdateExistingRoomAsync(viewModel.CreatedRoom);
            }
        }

        private async System.Threading.Tasks.Task UpdateExistingRoomAsync(Models.RoomDetailModel roomDetail)
        {
            try
            {
                // Convert RoomDetailModel to comprehensive RoomModel with all fields
                var room = new RoomModel
                {
                    // Basic Information
                    Id = SelectedRoom?.BaseRoom?.Id ?? roomDetail.Id,
                    RoomNumber = roomDetail.RoomNumber,
                    FloorNumber = roomDetail.FloorNumber,
                    TypeId = roomDetail.TypeId,
                    RoomSize = roomDetail.RoomSize,
                    Description = roomDetail.Description,
                    
                    // Status
                    CleanStatus = roomDetail.CleanStatus,
                    Status = roomDetail.Status,
                    AvailabilityStatus = roomDetail.AvailabilityStatus,
                    OperationalStatus = roomDetail.OperationalStatus,
                    
                    // Capacity & Bed Configuration
                    MaxOccupancy = roomDetail.MaxOccupancy,
                    NumberOfBeds = roomDetail.NumberOfBeds,
                    BedType = roomDetail.BedType,
                    HasExtraBed = roomDetail.HasExtraBed,
                    ExtraBedPrice = roomDetail.ExtraBedPrice,
                    
                    // Pricing
                    BasePricePerNight = roomDetail.BasePricePerNight,
                    WeekendPrice = roomDetail.WeekendPrice,
                    HolidayPrice = roomDetail.HolidayPrice,
                    Currency = roomDetail.Currency,
                    
                    // Amenities (already converted to JSON by the AmenitiesJson setter)
                    Amenities = roomDetail.Amenities ?? new System.Collections.Generic.List<string>(),
                    
                    // Images (already converted to JSON by the ImageUrlsJson setter)
                    ImageUrls = roomDetail.ImageUrls ?? new System.Collections.Generic.List<string>(),
                    MainImageUrl = roomDetail.MainImageUrl,
                    
                    // Housekeeping
                    LastCleanedDate = roomDetail.LastCleanedDate,
                    HousekeepingNotes = roomDetail.HousekeepingNotes,
                    
                    // Maintenance
                    MaintenanceIssue = roomDetail.MaintenanceIssue,
                    MaintenanceDate = roomDetail.MaintenanceDate,
                    
                    // Additional Attributes
                    SmokingAllowed = roomDetail.SmokingAllowed,
                    WheelchairAccessible = roomDetail.WheelchairAccessible,
                    PetFriendly = roomDetail.PetFriendly,
                    
                    // Notes
                    StaffNotes = roomDetail.StaffNotes,
                    InternalComments = roomDetail.InternalComments,
                    
                    // Timestamps
                    CreatedAt = SelectedRoom?.BaseRoom?.CreatedAt ?? DateTime.Now,
                    UpdatedAt = DateTime.Now
                };

                await DataStore.UpdateRoomAsync(room);
                LoadData();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error updating room: {ex.Message}");
            }
        }

        private async System.Threading.Tasks.Task SaveNewRoomAsync(Models.RoomDetailModel roomDetail)
        {
            try
            {
                // Convert RoomDetailModel to comprehensive RoomModel with all fields
                var room = new RoomModel
                {
                    // Basic Information
                    Id = roomDetail.Id,
                    RoomNumber = roomDetail.RoomNumber,
                    FloorNumber = roomDetail.FloorNumber,
                    TypeId = roomDetail.TypeId,
                    RoomSize = roomDetail.RoomSize,
                    Description = roomDetail.Description,
                    
                    // Status
                    CleanStatus = roomDetail.CleanStatus,
                    Status = roomDetail.Status,
                    AvailabilityStatus = roomDetail.AvailabilityStatus,
                    OperationalStatus = roomDetail.OperationalStatus,
                    
                    // Capacity & Bed Configuration
                    MaxOccupancy = roomDetail.MaxOccupancy,
                    NumberOfBeds = roomDetail.NumberOfBeds,
                    BedType = roomDetail.BedType,
                    HasExtraBed = roomDetail.HasExtraBed,
                    ExtraBedPrice = roomDetail.ExtraBedPrice,
                    
                    // Pricing
                    BasePricePerNight = roomDetail.BasePricePerNight,
                    WeekendPrice = roomDetail.WeekendPrice,
                    HolidayPrice = roomDetail.HolidayPrice,
                    Currency = roomDetail.Currency,
                    
                    // Amenities
                    Amenities = roomDetail.Amenities ?? new System.Collections.Generic.List<string>(),
                    
                    // Images
                    ImageUrls = roomDetail.ImageUrls ?? new System.Collections.Generic.List<string>(),
                    MainImageUrl = roomDetail.MainImageUrl,
                    
                    // Housekeeping
                    LastCleanedDate = roomDetail.LastCleanedDate,
                    HousekeepingNotes = roomDetail.HousekeepingNotes,
                    
                    // Maintenance
                    MaintenanceIssue = roomDetail.MaintenanceIssue,
                    MaintenanceDate = roomDetail.MaintenanceDate,
                    
                    // Additional Attributes
                    SmokingAllowed = roomDetail.SmokingAllowed,
                    WheelchairAccessible = roomDetail.WheelchairAccessible,
                    PetFriendly = roomDetail.PetFriendly,
                    
                    // Notes
                    StaffNotes = roomDetail.StaffNotes,
                    InternalComments = roomDetail.InternalComments,
                    
                    // Timestamps
                    CreatedAt = DateTime.Now,
                    UpdatedAt = null
                };

                await DataStore.AddRoomAsync(room);
                LoadData();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving room: {ex.Message}");
            }
        }

        private void PopulateForm(RoomDisplayModel display)
        {
            var r = display.BaseRoom;
            FormRoomNumber = r.RoomNumber;
            FormFloor = r.FloorNumber.ToString();
            FormSelectedType = RoomTypes.FirstOrDefault(t => t.Id == r.TypeId);
            FormCleanStatus = r.CleanStatus;
        }

        private void ViewRoomDetails(RoomDisplayModel room)
        {
            if (room == null) return;
            
            // Set the selected room and populate form
            SelectedRoom = room;
            PopulateForm(room);
            
            // Show details panel (not editing mode)
            IsEditing = false;
            IsViewingDetails = true;
        }

        private async void Save()
        {
            if (string.IsNullOrWhiteSpace(FormRoomNumber)) return;
            int.TryParse(FormFloor, out int floor);

            var room = (SelectedRoom == null) ? new RoomModel() : SelectedRoom.BaseRoom;
            room.RoomNumber = FormRoomNumber;
            room.FloorNumber = floor;
            room.TypeId = FormSelectedType?.Id;
            room.CleanStatus = FormCleanStatus;

            try 
            {
                if (SelectedRoom == null)
                {
                    room.Id = DataStore.GenerateId();
                    room.Status = "Available";
                    await ApiService.PostAsync<RoomModel>("rooms", room);
                }
                else
                {
                    await ApiService.PutAsync($"rooms/{room.Id}", room);
                }

                await DataStore.LoadAsync(); // Refresh local data
                IsEditing = false;
                IsViewingDetails = false;
                LoadData();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving room: {ex.Message}");
            }
        }

        private async void DeleteSelected()
        {
            if (!AuthService.CanDeleteRoom())
            {
                MessageBox.Show("Access Denied: Receptionist cannot delete rooms.");
                return;
            }

            if (SelectedRoom != null)
            {
                try 
                {
                    await ApiService.DeleteAsync($"rooms/{SelectedRoom.BaseRoom.Id}");
                    await DataStore.LoadAsync();
                    IsEditing = false;
                    IsViewingDetails = false;
                    LoadData();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error deleting room: {ex.Message}");
                }
            }
        }

        public void SelectRoomById(string roomId)
        {
            // Load data first to ensure rooms are available
            LoadData();
            
            // Find the room by ID
            var room = Rooms.FirstOrDefault(r => r.BaseRoom.Id == roomId);
            if (room != null)
            {
                SelectedRoom = room;
                PopulateForm(room);
                IsEditing = true;
            }
        }
    }
}
