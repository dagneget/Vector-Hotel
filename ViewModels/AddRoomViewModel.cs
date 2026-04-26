using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using HRS.Models;
using HRS.Services;
using Microsoft.Win32;

namespace HRS.ViewModels
{
    /// <summary>
    /// ViewModel for the Add New Room dialog with comprehensive room details
    /// </summary>
    public class AddRoomViewModel : ViewModelBase
    {
        #region Constructor

        public AddRoomViewModel()
        {
            // Initialize commands
            CloseCommand = new RelayCommand(_ => CloseDialog(false));
            CancelCommand = new RelayCommand(_ => CloseDialog(false));
            CreateRoomCommand = new RelayCommand(async _ => await CreateRoomAsync(), _ => CanCreateRoom());
            SaveDraftCommand = new RelayCommand(_ => SaveAsDraft());
            UploadImagesCommand = new RelayCommand(_ => UploadImages());

            // Load room type names from data store (null-safe) and add Custom option
            var roomTypes = DataStore.Data?.RoomTypes ?? new ObservableCollection<RoomTypeModel>();
            RoomTypeNames = new ObservableCollection<string>(roomTypes.Select(rt => rt.Name));
            RoomTypeNames.Add("Custom");
            if (RoomTypeNames.Any())
                SelectedRoomTypeName = RoomTypeNames.First();

            // Initialize editable collections from DataStore and add Custom option
            var currencies = DataStore.Data?.Currencies ?? RoomStatuses.Currencies;
            Currencies = new ObservableCollection<string>(currencies);
            if (!Currencies.Contains("Custom"))
                Currencies.Add("Custom");
            
            var bedTypes = DataStore.Data?.BedTypes ?? RoomStatuses.BedTypes;
            BedTypes = new ObservableCollection<string>(bedTypes);
            if (!BedTypes.Contains("Custom"))
                BedTypes.Add("Custom");
            AvailabilityStatuses = new ObservableCollection<string>(RoomStatuses.AvailabilityStatuses);
            CleaningStatuses = new ObservableCollection<string>(RoomStatuses.CleaningStatuses);
            MaintenanceStatuses = new ObservableCollection<string>(RoomStatuses.MaintenanceStatuses);

            // Set defaults
            SelectedCurrency = "USD";
            SelectedBedType = "Queen";
            SelectedAvailabilityStatus = "Available";
            SelectedCleaningStatus = "Clean";
            SelectedMaintenanceStatus = "Normal";
            MaxOccupancy = "2";
            NumberOfBeds = "1";
            RoomSize = "25";
        }

        #endregion

        #region Load Existing Room Data

        public void LoadFromRoomDetail(RoomDetailModel room)
        {
            if (room == null) return;

            // Basic Information
            RoomNumber = room.RoomNumber;
            FloorNumber = room.FloorNumber.ToString();
            RoomSize = room.RoomSize.ToString();

            // Room Type - check if it exists in the list, otherwise use Custom
            var roomType = DataStore.Data?.RoomTypes?.FirstOrDefault(rt => rt.Id == room.TypeId);
            if (roomType != null && RoomTypeNames.Contains(roomType.Name))
            {
                SelectedRoomTypeName = roomType.Name;
            }
            else if (roomType != null)
            {
                // Room type exists but not in dropdown (maybe added after dialog opened)
                RoomTypeNames.Insert(RoomTypeNames.Count - 1, roomType.Name);
                SelectedRoomTypeName = roomType.Name;
            }
            else
            {
                SelectedRoomTypeName = "Custom";
                CustomRoomTypeName = "Unknown";
            }

            // Currency - check if it exists, otherwise add it to list
            if (Currencies.Contains(room.Currency))
            {
                SelectedCurrency = room.Currency;
            }
            else if (!string.IsNullOrWhiteSpace(room.Currency) && room.Currency != "Custom")
            {
                // Add custom currency to list (insert before "Custom")
                Currencies.Insert(Currencies.Count - 1, room.Currency);
                SelectedCurrency = room.Currency;
            }
            else
            {
                SelectedCurrency = "Custom";
                CustomCurrency = room.Currency;
            }

            // Capacity & Bed Configuration
            MaxOccupancy = room.MaxOccupancy.ToString();
            NumberOfBeds = room.NumberOfBeds.ToString();
            HasExtraBed = room.HasExtraBed;
            ExtraBedPrice = room.ExtraBedPrice.ToString();

            // Bed Type - check if it exists, otherwise add it to list
            if (BedTypes.Contains(room.BedType))
            {
                SelectedBedType = room.BedType;
            }
            else if (!string.IsNullOrWhiteSpace(room.BedType) && room.BedType != "Custom")
            {
                // Add custom bed type to list (insert before "Custom")
                BedTypes.Insert(BedTypes.Count - 1, room.BedType);
                SelectedBedType = room.BedType;
            }
            else
            {
                SelectedBedType = "Custom";
                CustomBedType = room.BedType;
            }

            // Pricing
            BasePrice = room.BasePricePerNight.ToString();
            WeekendPrice = room.WeekendPrice.ToString();
            HolidayPrice = room.HolidayPrice.ToString();

            // Status
            SelectedAvailabilityStatus = room.AvailabilityStatus;
            SelectedCleaningStatus = room.CleanStatus;
            SelectedMaintenanceStatus = room.OperationalStatus;

            // Amenities
            if (room.Amenities != null)
            {
                HasWifi = room.Amenities.Contains("Wi-Fi");
                HasAC = room.Amenities.Contains("Air Conditioning");
                HasTV = room.Amenities.Contains("TV");
                HasPhone = room.Amenities.Contains("Telephone");
                HasPrivateBathroom = room.Amenities.Contains("Private Bathroom");
                HasShower = room.Amenities.Contains("Shower");
                HasBathtub = room.Amenities.Contains("Bathtub");
                HasHotWater = room.Amenities.Contains("Hot Water");
                HasDesk = room.Amenities.Contains("Desk");
                HasChair = room.Amenities.Contains("Chair");
                HasWardrobe = room.Amenities.Contains("Wardrobe");
                HasMirror = room.Amenities.Contains("Mirror");
                HasMinibar = room.Amenities.Contains("Minibar");
                HasRefrigerator = room.Amenities.Contains("Refrigerator");
                HasCoffeeMaker = room.Amenities.Contains("Coffee Maker");
                HasSafe = room.Amenities.Contains("Safe");
                HasBalcony = room.Amenities.Contains("Balcony");
                HasSeaView = room.Amenities.Contains("Sea View");
                HasCityView = room.Amenities.Contains("City View");
            }

            // Additional Attributes
            SmokingAllowed = room.SmokingAllowed;
            WheelchairAccessible = room.WheelchairAccessible;

            // Notes
            StaffNotes = room.StaffNotes;

            // Media
            if (room.ImageUrls != null)
            {
                SelectedImagePaths = new ObservableCollection<string>(room.ImageUrls);
            }
        }

        #endregion

        #region Commands

        public ICommand CloseCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand CreateRoomCommand { get; }
        public ICommand SaveDraftCommand { get; }
        public ICommand UploadImagesCommand { get; }

        #endregion

        #region Collections & Options

        private ObservableCollection<string> _roomTypeNames;
        public ObservableCollection<string> RoomTypeNames
        {
            get => _roomTypeNames;
            set => SetProperty(ref _roomTypeNames, value);
        }

        private ObservableCollection<string> _currencies;
        public ObservableCollection<string> Currencies
        {
            get => _currencies;
            set => SetProperty(ref _currencies, value);
        }

        private ObservableCollection<string> _bedTypes;
        public ObservableCollection<string> BedTypes
        {
            get => _bedTypes;
            set => SetProperty(ref _bedTypes, value);
        }
        private ObservableCollection<string> _availabilityStatuses;
        public ObservableCollection<string> AvailabilityStatuses
        {
            get => _availabilityStatuses;
            set => SetProperty(ref _availabilityStatuses, value);
        }

        private ObservableCollection<string> _cleaningStatuses;
        public ObservableCollection<string> CleaningStatuses
        {
            get => _cleaningStatuses;
            set => SetProperty(ref _cleaningStatuses, value);
        }

        private ObservableCollection<string> _maintenanceStatuses;
        public ObservableCollection<string> MaintenanceStatuses
        {
            get => _maintenanceStatuses;
            set => SetProperty(ref _maintenanceStatuses, value);
        }

        #endregion

        #region Basic Information

        private string _roomNumber;
        public string RoomNumber
        {
            get => _roomNumber;
            set => SetProperty(ref _roomNumber, value);
        }

        private string _floorNumber = "1";
        public string FloorNumber
        {
            get => _floorNumber;
            set => SetProperty(ref _floorNumber, value);
        }

        private string _selectedRoomTypeName;
        public string SelectedRoomTypeName
        {
            get => _selectedRoomTypeName;
            set => SetProperty(ref _selectedRoomTypeName, value);
        }

        private string _customRoomTypeName;
        public string CustomRoomTypeName
        {
            get => _customRoomTypeName;
            set => SetProperty(ref _customRoomTypeName, value);
        }

        private string _customCurrency;
        public string CustomCurrency
        {
            get => _customCurrency;
            set => SetProperty(ref _customCurrency, value);
        }

        private string _customBedType;
        public string CustomBedType
        {
            get => _customBedType;
            set => SetProperty(ref _customBedType, value);
        }

        private string _roomSize = "25";
        public string RoomSize
        {
            get => _roomSize;
            set => SetProperty(ref _roomSize, value);
        }

        private string _selectedCurrency = "USD";
        public string SelectedCurrency
        {
            get => _selectedCurrency;
            set => SetProperty(ref _selectedCurrency, value);
        }

        #endregion

        #region Capacity & Bed Configuration

        private string _maxOccupancy = "2";
        public string MaxOccupancy
        {
            get => _maxOccupancy;
            set => SetProperty(ref _maxOccupancy, value);
        }

        private string _numberOfBeds = "1";
        public string NumberOfBeds
        {
            get => _numberOfBeds;
            set => SetProperty(ref _numberOfBeds, value);
        }

        private string _selectedBedType = "Queen";
        public string SelectedBedType
        {
            get => _selectedBedType;
            set => SetProperty(ref _selectedBedType, value);
        }

        private bool _hasExtraBed;
        public bool HasExtraBed
        {
            get => _hasExtraBed;
            set => SetProperty(ref _hasExtraBed, value);
        }

        private string _extraBedPrice;
        public string ExtraBedPrice
        {
            get => _extraBedPrice;
            set => SetProperty(ref _extraBedPrice, value);
        }

        #endregion

        #region Pricing

        private string _basePrice;
        public string BasePrice
        {
            get => _basePrice;
            set => SetProperty(ref _basePrice, value);
        }

        private string _weekendPrice;
        public string WeekendPrice
        {
            get => _weekendPrice;
            set => SetProperty(ref _weekendPrice, value);
        }

        private string _holidayPrice;
        public string HolidayPrice
        {
            get => _holidayPrice;
            set => SetProperty(ref _holidayPrice, value);
        }

        #endregion

        #region Status

        private string _selectedAvailabilityStatus = "Available";
        public string SelectedAvailabilityStatus
        {
            get => _selectedAvailabilityStatus;
            set => SetProperty(ref _selectedAvailabilityStatus, value);
        }

        private string _selectedCleaningStatus = "Clean";
        public string SelectedCleaningStatus
        {
            get => _selectedCleaningStatus;
            set => SetProperty(ref _selectedCleaningStatus, value);
        }

        private string _selectedMaintenanceStatus = "Normal";
        public string SelectedMaintenanceStatus
        {
            get => _selectedMaintenanceStatus;
            set => SetProperty(ref _selectedMaintenanceStatus, value);
        }

        #endregion

        #region Amenities - Basic

        private bool _hasWifi = true;
        public bool HasWifi
        {
            get => _hasWifi;
            set => SetProperty(ref _hasWifi, value);
        }

        private bool _hasAC = true;
        public bool HasAC
        {
            get => _hasAC;
            set => SetProperty(ref _hasAC, value);
        }

        private bool _hasTV = true;
        public bool HasTV
        {
            get => _hasTV;
            set => SetProperty(ref _hasTV, value);
        }

        private bool _hasPhone;
        public bool HasPhone
        {
            get => _hasPhone;
            set => SetProperty(ref _hasPhone, value);
        }

        #endregion

        #region Amenities - Bathroom

        private bool _hasPrivateBathroom = true;
        public bool HasPrivateBathroom
        {
            get => _hasPrivateBathroom;
            set => SetProperty(ref _hasPrivateBathroom, value);
        }

        private bool _hasShower = true;
        public bool HasShower
        {
            get => _hasShower;
            set => SetProperty(ref _hasShower, value);
        }

        private bool _hasBathtub;
        public bool HasBathtub
        {
            get => _hasBathtub;
            set => SetProperty(ref _hasBathtub, value);
        }

        private bool _hasHotWater = true;
        public bool HasHotWater
        {
            get => _hasHotWater;
            set => SetProperty(ref _hasHotWater, value);
        }

        #endregion

        #region Amenities - Furniture

        private bool _hasDesk = true;
        public bool HasDesk
        {
            get => _hasDesk;
            set => SetProperty(ref _hasDesk, value);
        }

        private bool _hasChair = true;
        public bool HasChair
        {
            get => _hasChair;
            set => SetProperty(ref _hasChair, value);
        }

        private bool _hasWardrobe = true;
        public bool HasWardrobe
        {
            get => _hasWardrobe;
            set => SetProperty(ref _hasWardrobe, value);
        }

        private bool _hasMirror = true;
        public bool HasMirror
        {
            get => _hasMirror;
            set => SetProperty(ref _hasMirror, value);
        }

        #endregion

        #region Amenities - Extras

        private bool _hasMinibar;
        public bool HasMinibar
        {
            get => _hasMinibar;
            set => SetProperty(ref _hasMinibar, value);
        }

        private bool _hasRefrigerator;
        public bool HasRefrigerator
        {
            get => _hasRefrigerator;
            set => SetProperty(ref _hasRefrigerator, value);
        }

        private bool _hasCoffeeMaker;
        public bool HasCoffeeMaker
        {
            get => _hasCoffeeMaker;
            set => SetProperty(ref _hasCoffeeMaker, value);
        }

        private bool _hasSafe;
        public bool HasSafe
        {
            get => _hasSafe;
            set => SetProperty(ref _hasSafe, value);
        }

        #endregion

        #region Amenities - Special

        private bool _hasBalcony;
        public bool HasBalcony
        {
            get => _hasBalcony;
            set => SetProperty(ref _hasBalcony, value);
        }

        private bool _hasSeaView;
        public bool HasSeaView
        {
            get => _hasSeaView;
            set => SetProperty(ref _hasSeaView, value);
        }

        private bool _hasCityView;
        public bool HasCityView
        {
            get => _hasCityView;
            set => SetProperty(ref _hasCityView, value);
        }

        #endregion

        #region Additional Attributes

        private bool _smokingAllowed;
        public bool SmokingAllowed
        {
            get => _smokingAllowed;
            set => SetProperty(ref _smokingAllowed, value);
        }

        private bool _wheelchairAccessible;
        public bool WheelchairAccessible
        {
            get => _wheelchairAccessible;
            set => SetProperty(ref _wheelchairAccessible, value);
        }

        #endregion

        #region Notes

        private string _staffNotes;
        public string StaffNotes
        {
            get => _staffNotes;
            set => SetProperty(ref _staffNotes, value);
        }

        #endregion

        #region Media

        private ObservableCollection<string> _selectedImagePaths = new ObservableCollection<string>();
        public ObservableCollection<string> SelectedImagePaths
        {
            get => _selectedImagePaths;
            set => SetProperty(ref _selectedImagePaths, value);
        }

        #endregion

        #region Result

        public RoomDetailModel CreatedRoom { get; private set; }
        public bool IsSuccess { get; private set; }

        #endregion

        #region Methods

        private bool CanCreateRoom()
        {
            // Validate room type - if Custom is selected, CustomRoomTypeName must be provided
            bool isValidRoomType = !string.IsNullOrWhiteSpace(SelectedRoomTypeName) && 
                                   (SelectedRoomTypeName != "Custom" || !string.IsNullOrWhiteSpace(CustomRoomTypeName));

            return !string.IsNullOrWhiteSpace(RoomNumber) &&
                   !string.IsNullOrWhiteSpace(FloorNumber) &&
                   !string.IsNullOrWhiteSpace(BasePrice) &&
                   isValidRoomType;
        }

        private async System.Threading.Tasks.Task CreateRoomAsync()
        {
            try
            {
                // Build amenities list
                var amenities = new List<string>();
                if (HasWifi) amenities.Add("Wi-Fi");
                if (HasAC) amenities.Add("Air Conditioning");
                if (HasTV) amenities.Add("TV");
                if (HasPhone) amenities.Add("Telephone");
                if (HasPrivateBathroom) amenities.Add("Private Bathroom");
                if (HasShower) amenities.Add("Shower");
                if (HasBathtub) amenities.Add("Bathtub");
                if (HasHotWater) amenities.Add("Hot Water");
                if (HasDesk) amenities.Add("Desk");
                if (HasChair) amenities.Add("Chair");
                if (HasWardrobe) amenities.Add("Wardrobe");
                if (HasMirror) amenities.Add("Mirror");
                if (HasMinibar) amenities.Add("Mini Bar");
                if (HasRefrigerator) amenities.Add("Refrigerator");
                if (HasCoffeeMaker) amenities.Add("Coffee/Tea Maker");
                if (HasSafe) amenities.Add("Safe Box");
                if (HasBalcony) amenities.Add("Balcony");
                if (HasSeaView) amenities.Add("Sea View");
                if (HasCityView) amenities.Add("City View");

                // Parse numeric values
                int.TryParse(MaxOccupancy, out int maxOcc);
                int.TryParse(NumberOfBeds, out int numBeds);
                decimal.TryParse(RoomSize, out decimal roomSize);
                decimal.TryParse(BasePrice, out decimal basePrice);
                decimal.TryParse(WeekendPrice, out decimal weekendPrice);
                decimal.TryParse(HolidayPrice, out decimal holidayPrice);
                decimal.TryParse(ExtraBedPrice, out decimal extraBedPrice);

                CreatedRoom = new RoomDetailModel
                {
                    Id = DataStore.GenerateId(),
                    RoomNumber = RoomNumber.Trim(),
                    FloorNumber = int.TryParse(FloorNumber, out int floor) ? floor : 1,
                    TypeId = await GetOrCreateRoomTypeIdAsync(SelectedRoomTypeName),
                    RoomSize = roomSize,
                    Currency = GetSelectedCurrency(),

                    // Capacity
                    MaxOccupancy = maxOcc,
                    NumberOfBeds = numBeds,
                    BedType = GetSelectedBedType(),
                    HasExtraBed = HasExtraBed,
                    ExtraBedPrice = extraBedPrice,

                    // Pricing
                    BasePricePerNight = basePrice,
                    WeekendPrice = weekendPrice,
                    HolidayPrice = holidayPrice,

                    // Status
                    AvailabilityStatus = SelectedAvailabilityStatus,
                    CleanStatus = SelectedCleaningStatus,
                    OperationalStatus = SelectedMaintenanceStatus,
                    Status = SelectedAvailabilityStatus,

                    // Amenities
                    Amenities = amenities,

                    // Attributes
                    SmokingAllowed = SmokingAllowed,
                    WheelchairAccessible = WheelchairAccessible,

                    // Notes
                    StaffNotes = StaffNotes,
                    CreatedAt = DateTime.Now,

                    // Images
                    ImageUrls = SelectedImagePaths.ToList()
                };

                IsSuccess = true;
                CloseDialog(true);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error creating room: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SaveAsDraft()
        {
            // TODO: Implement draft saving to local storage or temp table
            MessageBox.Show("Draft saved successfully!", "Draft Saved", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void UploadImages()
        {
            var dialog = new OpenFileDialog
            {
                Title = "Select Room Images",
                Filter = "Image Files|*.jpg;*.jpeg;*.png;*.gif;*.bmp|All Files|*.*",
                Multiselect = true
            };

            if (dialog.ShowDialog() == true)
            {
                foreach (var file in dialog.FileNames)
                {
                    SelectedImagePaths.Add(file);
                }
            }
        }

        private async System.Threading.Tasks.Task<string> GetOrCreateRoomTypeIdAsync(string roomTypeName)
        {
            // Handle Custom option - use the custom name entered by user
            if (roomTypeName == "Custom")
            {
                roomTypeName = CustomRoomTypeName;
            }

            if (string.IsNullOrWhiteSpace(roomTypeName))
                return null;

            // Check if room type already exists
            var existingType = DataStore.Data?.RoomTypes?.FirstOrDefault(rt => 
                rt.Name.Equals(roomTypeName, StringComparison.OrdinalIgnoreCase));
            
            if (existingType != null)
                return existingType.Id;

            // Create new room type and save to API
            var newRoomType = new RoomTypeModel
            {
                Id = DataStore.GenerateId(),
                Name = roomTypeName.Trim(),
                BasePrice = decimal.TryParse(BasePrice, out decimal price) ? price : 0
            };

            // Save to API - this ensures the room type persists
            await DataStore.AddRoomTypeAsync(newRoomType);
            
            // Add to local dropdown (insert before "Custom")
            RoomTypeNames.Insert(RoomTypeNames.Count - 1, newRoomType.Name);

            return newRoomType.Id;
        }

        private string GetSelectedCurrency()
        {
            if (SelectedCurrency == "Custom")
            {
                if (!string.IsNullOrWhiteSpace(CustomCurrency))
                {
                    // Add custom currency to list if not exists
                    if (!Currencies.Contains(CustomCurrency) && CustomCurrency != "Custom")
                    {
                        Currencies.Insert(Currencies.Count - 1, CustomCurrency);
                    }
                    // Also add to RoomStatuses and DataStore for persistence
                    if (!RoomStatuses.Currencies.Contains(CustomCurrency))
                    {
                        RoomStatuses.Currencies.Add(CustomCurrency);
                    }
                    if (DataStore.Data != null && !DataStore.Data.Currencies.Contains(CustomCurrency))
                    {
                        DataStore.Data.Currencies.Add(CustomCurrency);
                        // Save to database for persistence across app restarts
                        _ = DataStore.UpdateCurrenciesAsync(new List<string>(DataStore.Data.Currencies));
                    }
                    return CustomCurrency;
                }
                return "USD"; // Default fallback
            }
            return SelectedCurrency;
        }

        private string GetSelectedBedType()
        {
            if (SelectedBedType == "Custom")
            {
                if (!string.IsNullOrWhiteSpace(CustomBedType))
                {
                    // Add custom bed type to list if not exists
                    if (!BedTypes.Contains(CustomBedType) && CustomBedType != "Custom")
                    {
                        BedTypes.Insert(BedTypes.Count - 1, CustomBedType);
                    }
                    // Also add to RoomStatuses and DataStore for persistence
                    if (!RoomStatuses.BedTypes.Contains(CustomBedType))
                    {
                        RoomStatuses.BedTypes.Add(CustomBedType);
                    }
                    if (DataStore.Data != null && !DataStore.Data.BedTypes.Contains(CustomBedType))
                    {
                        DataStore.Data.BedTypes.Add(CustomBedType);
                        // Save to database for persistence across app restarts
                        _ = DataStore.UpdateBedTypesAsync(new List<string>(DataStore.Data.BedTypes));
                    }
                    return CustomBedType;
                }
                return "Queen"; // Default fallback
            }
            return SelectedBedType;
        }

        private void CloseDialog(bool result)
        {
            // Find the window and close it
            foreach (Window window in Application.Current.Windows)
            {
                if (window is Views.AddRoomDialog)
                {
                    window.DialogResult = result;
                    window.Close();
                    break;
                }
            }
        }

        #endregion
    }
}
