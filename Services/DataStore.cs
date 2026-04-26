using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
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
        
        // Lookup Lists
        public List<string> Currencies { get; set; } = new List<string> { "USD", "EUR", "GBP", "JPY", "AED", "SAR" };
        public List<string> BedTypes { get; set; } = new List<string> { "Single", "Twin", "Queen", "King" };
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
                
                // Load settings (currencies and bed types)
                try
                {
                    var settings = await ApiService.GetAsync<SettingsModel>("settings");
                    if (settings?.Currencies?.Count > 0)
                        Data.Currencies = settings.Currencies;
                    if (settings?.BedTypes?.Count > 0)
                        Data.BedTypes = settings.BedTypes;
                }
                catch (Exception ex) 
                { 
                    Console.WriteLine($"Settings load error: {ex.Message}");
                    /* Use defaults - settings endpoint may not exist yet */ 
                }
                
                // Sync RoomStatuses with loaded data
                RoomStatuses.SyncWithDataStore();
                
                EventBus.Instance.PublishDataChanged();
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

        [Obsolete("Use specific entity methods (Add/Update/Delete) instead. This method only notifies UI of changes.")]
        public static void Save()
        {
            EventBus.Instance.PublishDataChanged();
        }

        // --- Customers ---
        public static async Task AddCustomerAsync(CustomerModel customer)
        {
            var newCustomer = await ApiService.PostAsync<CustomerModel>("customers", customer);
            Data.Customers.Add(newCustomer);
            Save();
        }

        public static async Task UpdateCustomerAsync(CustomerModel customer)
        {
            await ApiService.PutAsync($"customers/{customer.Id}", customer);
            var idx = Data.Customers.IndexOf(Data.Customers.First(c => c.Id == customer.Id));
            if (idx >= 0) Data.Customers[idx] = customer;
            Save();
        }

        public static async Task DeleteCustomerAsync(string id)
        {
            await ApiService.DeleteAsync($"customers/{id}");
            var customer = Data.Customers.FirstOrDefault(c => c.Id == id);
            if (customer != null) Data.Customers.Remove(customer);
            Save();
        }

        // --- Room Types ---
        public static async Task AddRoomTypeAsync(RoomTypeModel roomType)
        {
            var newRoomType = await ApiService.PostAsync<RoomTypeModel>("roomtypes", roomType);
            Data.RoomTypes.Add(newRoomType);
            Save();
        }

        // --- Rooms ---
        public static async Task AddRoomAsync(RoomModel room)
        {
            var newRoom = await ApiService.PostAsync<RoomModel>("rooms", room);
            Data.Rooms.Add(newRoom);
            Save();
        }

        public static async Task UpdateRoomAsync(RoomModel room)
        {
            await ApiService.PutAsync($"rooms/{room.Id}", room);
            var idx = Data.Rooms.IndexOf(Data.Rooms.First(r => r.Id == room.Id));
            if (idx >= 0) Data.Rooms[idx] = room;
            Save();
        }

        public static async Task DeleteRoomAsync(string id)
        {
            await ApiService.DeleteAsync($"rooms/{id}");
            var room = Data.Rooms.FirstOrDefault(r => r.Id == id);
            if (room != null) Data.Rooms.Remove(room);
            Save();
        }

        // --- Reservations ---
        public static async Task AddReservationAsync(ReservationModel res)
        {
            var newRes = await ApiService.PostAsync<ReservationModel>("reservations", res);
            Data.Reservations.Add(newRes);
            Save();
        }

        public static async Task UpdateReservationAsync(ReservationModel res)
        {
            await ApiService.PutAsync($"reservations/{res.Id}", res);
            var idx = Data.Reservations.IndexOf(Data.Reservations.First(r => r.Id == res.Id));
            if (idx >= 0) Data.Reservations[idx] = res;
            Save();
        }

        public static async Task UpdateReservationStatusAsync(string id, string newStatus)
        {
            await ApiService.PutAsync($"reservations/{id}/status", newStatus);
            await LoadAsync();
        }

        public static async Task DeleteReservationAsync(string id)
        {
            await ApiService.DeleteAsync($"reservations/{id}");
            var res = Data.Reservations.FirstOrDefault(r => r.Id == id);
            if (res != null) Data.Reservations.Remove(res);
            Save();
        }

        // --- Payments ---
        public static async Task AddPaymentAsync(PaymentModel payment)
        {
            var newPayment = await ApiService.PostAsync<PaymentModel>("payments", payment);
            Data.Payments.Add(newPayment);
            Save();
        }

        public static async Task DeletePaymentAsync(string id)
        {
            await ApiService.DeleteAsync($"payments/{id}");
            var payment = Data.Payments.FirstOrDefault(p => p.Id == id);
            if (payment != null) Data.Payments.Remove(payment);
            Save();
        }

        // --- Settings / Lookups ---
        public static async Task SaveSettingsAsync(SettingsModel settings)
        {
            try
            {
                // Ensure JSON properties are set before sending
                settings.CurrenciesJson = Newtonsoft.Json.JsonConvert.SerializeObject(settings.Currencies ?? Data.Currencies);
                settings.BedTypesJson = Newtonsoft.Json.JsonConvert.SerializeObject(settings.BedTypes ?? Data.BedTypes);
                
                // Try to save as a single settings object
                await ApiService.PutAsync("settings", settings);
                Data.Currencies = settings.Currencies;
                Data.BedTypes = settings.BedTypes;
                Save();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving settings: {ex.Message}");
            }
        }
        
        public static async Task UpdateCurrenciesAsync(List<string> currencies)
        {
            try
            {
                var settings = new SettingsModel 
                { 
                    Id = 1,
                    Currencies = currencies,
                    BedTypes = Data.BedTypes 
                };
                // Explicitly set JSON properties for backend compatibility
                settings.CurrenciesJson = Newtonsoft.Json.JsonConvert.SerializeObject(currencies);
                settings.BedTypesJson = Newtonsoft.Json.JsonConvert.SerializeObject(Data.BedTypes);
                
                await SaveSettingsAsync(settings);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving currencies: {ex.Message}");
            }
        }
        
        public static async Task UpdateBedTypesAsync(List<string> bedTypes)
        {
            try
            {
                var settings = new SettingsModel 
                { 
                    Id = 1,
                    Currencies = Data.Currencies,
                    BedTypes = bedTypes 
                };
                // Explicitly set JSON properties for backend compatibility
                settings.CurrenciesJson = Newtonsoft.Json.JsonConvert.SerializeObject(Data.Currencies);
                settings.BedTypesJson = Newtonsoft.Json.JsonConvert.SerializeObject(bedTypes);
                
                await SaveSettingsAsync(settings);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving bed types: {ex.Message}");
            }
        }

        // --- Charges ---
        public static async Task AddChargeAsync(ChargeModel charge)
        {
            var newCharge = await ApiService.PostAsync<ChargeModel>("charges", charge);
            Data.Charges.Add(newCharge);
            Save();
        }

        public static async Task DeleteChargeAsync(string id)
        {
            await ApiService.DeleteAsync($"charges/{id}");
            var charge = Data.Charges.FirstOrDefault(c => c.Id == id);
            if (charge != null) Data.Charges.Remove(charge);
            Save();
        }

        // --- Users ---
        public static async Task AddUserAsync(UserModel user)
        {
            var newUser = await ApiService.PostAsync<UserModel>("users", user);
            Data.Users.Add(newUser);
            Save();
        }

        public static async Task UpdateUserAsync(UserModel user)
        {
            await ApiService.PutAsync($"users/{user.Id}", user);
            var idx = Data.Users.IndexOf(Data.Users.First(u => u.Id == user.Id));
            if (idx >= 0) Data.Users[idx] = user;
            Save();
        }

        public static async Task DeleteUserAsync(string id)
        {
            await ApiService.DeleteAsync($"users/{id}");
            var user = Data.Users.FirstOrDefault(u => u.Id == id);
            if (user != null) Data.Users.Remove(user);
            Save();
        }
    }
}
