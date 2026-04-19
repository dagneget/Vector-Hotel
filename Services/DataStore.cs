using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using HRS.Models;

namespace HRS.Services
{
    public class RootData
    {
        public ObservableCollection<UserModel> Users { get; set; } = new ObservableCollection<UserModel>();
        public ObservableCollection<CustomerModel> Customers { get; set; } = new ObservableCollection<CustomerModel>();
        public ObservableCollection<RoomTypeModel> RoomTypes { get; set; } = new ObservableCollection<RoomTypeModel>();
        public ObservableCollection<RoomModel> Rooms { get; set; } = new ObservableCollection<RoomModel>();
        public ObservableCollection<ReservationModel> Reservations { get; set; } = new ObservableCollection<ReservationModel>();
        public ObservableCollection<PaymentModel> Payments { get; set; } = new ObservableCollection<PaymentModel>();
        public ObservableCollection<ChargeModel> Charges { get; set; } = new ObservableCollection<ChargeModel>();
        public ObservableCollection<AuditLogModel> AuditLogs { get; set; } = new ObservableCollection<AuditLogModel>();
    }

    public static class DataStore
    {
        public static RootData Data { get; set; } = new RootData();
        
        public static string GenerateId() => Guid.NewGuid().ToString("N");

        public static async Task LoadAsync()
        {
            try
            {
                var users = await ApiService.GetAsync<List<UserModel>>("users");
                Data.Users = new ObservableCollection<UserModel>(users);

                var customers = await ApiService.GetAsync<List<CustomerModel>>("customers");
                Data.Customers = new ObservableCollection<CustomerModel>(customers);

                var roomTypes = await ApiService.GetAsync<List<RoomTypeModel>>("roomtypes");
                Data.RoomTypes = new ObservableCollection<RoomTypeModel>(roomTypes);

                var rooms = await ApiService.GetAsync<List<RoomModel>>("rooms");
                Data.Rooms = new ObservableCollection<RoomModel>(rooms);

                var reservations = await ApiService.GetAsync<List<ReservationModel>>("reservations");
                Data.Reservations = new ObservableCollection<ReservationModel>(reservations);

                var payments = await ApiService.GetAsync<List<PaymentModel>>("payments");
                Data.Payments = new ObservableCollection<PaymentModel>(payments);

                var charges = await ApiService.GetAsync<List<ChargeModel>>("charges");
                Data.Charges = new ObservableCollection<ChargeModel>(charges);
            }
            catch (Exception ex)
            {
                // In a real app, show a message box. For now, we'll log it.
                Console.WriteLine($"API Load Error: {ex.Message}");
            }
        }

        public static void Load()
        {
            // Sync wrapper for startup if needed, but better to call LoadAsync in App.xaml.cs
            Task.Run(async () => await LoadAsync()).Wait();
        }

        public static void Save()
        {
            // The old Save() was for EF6 SaveChanges. 
            // In the API version, we should ideally have Save methods per entity.
            // For now, we'll trigger an event to notify UI.
            EventBus.Instance.PublishDataChanged();
        }

        // --- Helper methods for API operations ---
        public static async Task AddRoomAsync(RoomModel room)
        {
            var newRoom = await ApiService.PostAsync<RoomModel>("rooms", room);
            Data.Rooms.Add(newRoom);
            Save();
        }

        public static async Task AddReservationAsync(ReservationModel res)
        {
            var newRes = await ApiService.PostAsync<ReservationModel>("reservations", res);
            Data.Reservations.Add(newRes);
            Save();
        }

        public static async Task UpdateRoomAsync(RoomModel room)
        {
            await ApiService.PutAsync($"rooms/{room.Id}", room);
            Save();
        }
    }
}
