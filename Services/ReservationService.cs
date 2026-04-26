using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HRS.Models;

namespace HRS.Services
{
    public static class ReservationService
    {
        public static bool IsRoomAvailable(string roomId, DateTime checkIn, DateTime checkOut, string excludeReservationId = null)
        {
            var overlapping = DataStore.Data.Reservations.Where(r => 
                r.RoomId == roomId && 
                r.RoomStatus != "Cancelled" && 
                r.RoomStatus != "CheckedOut" &&
                r.Id != excludeReservationId &&
                (
                    (checkIn >= r.CheckIn && checkIn < r.CheckOut) ||
                    (checkOut > r.CheckIn && checkOut <= r.CheckOut) ||
                    (checkIn <= r.CheckIn && checkOut >= r.CheckOut)
                )
            ).Any();

            return !overlapping;
        }

        public static decimal CalculatePrice(string roomId, DateTime checkIn, DateTime checkOut)
        {
            var room = DataStore.Data.Rooms.FirstOrDefault(r => r.Id == roomId);
            if (room == null) return 0;
            
            var type = DataStore.Data.RoomTypes.FirstOrDefault(t => t.Id == room.TypeId);
            decimal basePrice = type != null ? type.BasePrice : 0;
            
            int days = (int)(checkOut.Date - checkIn.Date).TotalDays;
            if (days <= 0) days = 1; // Minimum 1 night
            
            return basePrice * days;
        }
        
        public static List<RoomModel> GetAvailableRooms(DateTime checkIn, DateTime checkOut)
        {
            return DataStore.Data.Rooms.Where(r => IsRoomAvailable(r.Id, checkIn, checkOut)).ToList();
        }

        public static async Task<bool> ChangeReservationStateAsync(ReservationModel res, string newStatus)
        {
            if ((newStatus == "Pending" || newStatus == "Confirmed") && res.PaymentStatus == newStatus) return true;
            if ((newStatus == "CheckedIn" || newStatus == "CheckedOut" || newStatus == "Cancelled") && res.RoomStatus == newStatus) return true;

            try 
            {
                await ApiService.PutAsync($"reservations/{res.Id}/status", newStatus);
                await DataStore.LoadAsync(); // Refresh local cache to see room status changes
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Status Change Error: {ex.Message}");
                return false;
            }
        }
    }
}
