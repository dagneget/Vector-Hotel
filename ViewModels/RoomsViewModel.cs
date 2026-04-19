using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using HRS.Models;
using HRS.Services;

namespace HRS.ViewModels
{
    public class RoomDisplayModel : ViewModelBase
    {
        public RoomModel BaseRoom { get; set; }
        
        public string RoomNumber => BaseRoom.RoomNumber;
        public int FloorNumber => BaseRoom.FloorNumber;
        public string CleanStatus => BaseRoom.CleanStatus;
        public string Status => BaseRoom.Status;
        
        // Joined Data
        public string CategoryName { get; set; }
        public decimal BasePrice { get; set; }
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
                    IsEditing = (value != null);
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

        public RoomsViewModel()
        {
            SaveCommand = new RelayCommand(_ => Save());
            CancelEditCommand = new RelayCommand(_ => IsEditing = false);
            RegisterRoomCommand = new RelayCommand(_ => PrepareNew());
            DeleteRoomCommand = new RelayCommand(_ => DeleteSelected());

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
            SelectedRoom = null;
            FormRoomNumber = "";
            FormFloor = "1";
            FormSelectedType = RoomTypes.FirstOrDefault();
            FormCleanStatus = "Clean";
            IsEditing = true;
        }

        private void PopulateForm(RoomDisplayModel display)
        {
            var r = display.BaseRoom;
            FormRoomNumber = r.RoomNumber;
            FormFloor = r.FloorNumber.ToString();
            FormSelectedType = RoomTypes.FirstOrDefault(t => t.Id == r.TypeId);
            FormCleanStatus = r.CleanStatus;
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
                    LoadData();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error deleting room: {ex.Message}");
                }
            }
        }
    }
}
