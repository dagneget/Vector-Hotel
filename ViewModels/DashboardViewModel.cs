using System;
using System.Collections.ObjectModel;
using System.Linq;
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
                var activeRes = DataStore.Data.Reservations.Where(r => r.Status == "CheckedIn" || r.Status == "CheckedOut").ToList();
                if (!activeRes.Any()) return "$0";
                return $"${activeRes.Average(r => r.TotalPrice):F0}";
            }
        }

        public string RevparText 
        {
            get {
                var totalRooms = DataStore.Data.Rooms.Count();
                if (totalRooms == 0) return "$0";
                var totalRev = DataStore.Data.Reservations.Where(r => r.Status == "CheckedIn" || r.Status == "CheckedOut").Sum(r => r.TotalPrice);
                return $"${(totalRev / totalRooms):F0}";
            }
        }

        public string ArrivalsText => DataStore.Data.Reservations.Count(r => r.CheckIn.Date == DateTime.Today).ToString();

        private ObservableCollection<ReservationDisplayModel> _recentReservations;
        public ObservableCollection<ReservationDisplayModel> RecentReservations
        {
            get => _recentReservations;
            set => SetProperty(ref _recentReservations, value);
        }

        private ObservableCollection<FeedItem> _frontDeskFeed;
        public ObservableCollection<FeedItem> FrontDeskFeed
        {
            get => _frontDeskFeed;
            set => SetProperty(ref _frontDeskFeed, value);
        }

        public int TotalRooms => DataStore.Data.Rooms.Count();
        public int OccupiedRooms => DataStore.Data.Rooms.Count(r => r.Status == "Occupied" || r.Status == "Reserved");
        public int AvailableRooms => DataStore.Data.Rooms.Count(r => r.Status == "Available" || r.Status == "Clean");
        public int MaintenanceRooms => DataStore.Data.Rooms.Count(r => r.Status == "Maintenance" || r.Status == "Dirty");

        public DashboardViewModel()
        {
            LoadData();
            // Subscribe to data changes to refresh dashboard
            EventBus.Instance.DataChanged += () => LoadData();
        }

        private void LoadData()
        {
            var query = DataStore.Data.Reservations.Select(r => new ReservationDisplayModel
            {
                BaseReservation = r,
                CustomerName = DataStore.Data.Customers.FirstOrDefault(c => c.Id == r.CustomerId)?.FullName ?? "Unknown Guest",
                RoomNumber = DataStore.Data.Rooms.FirstOrDefault(room => room.Id == r.RoomId)?.RoomNumber ?? "Unassigned"
            });

            RecentReservations = new ObservableCollection<ReservationDisplayModel>(query.OrderByDescending(r => r.CheckInDate).Take(3));

            // Start with empty feed for fresh system
            FrontDeskFeed = new ObservableCollection<FeedItem>();

            // Notify UI of header changes
            OnPropertyChanged(nameof(OccupancyText));
            OnPropertyChanged(nameof(AdrText));
            OnPropertyChanged(nameof(RevparText));
            OnPropertyChanged(nameof(ArrivalsText));
            OnPropertyChanged(nameof(TotalRooms));
            OnPropertyChanged(nameof(OccupiedRooms));
            OnPropertyChanged(nameof(AvailableRooms));
            OnPropertyChanged(nameof(MaintenanceRooms));
        }
    }
}
