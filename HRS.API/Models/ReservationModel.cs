using System;

namespace HRS.API.Models
{
    public class ReservationModel
    {
        public string Id { get; set; }
        public string CustomerId { get; set; }
        public string RoomId { get; set; }
        public DateTime CheckIn { get; set; }
        public DateTime CheckOut { get; set; }
        public int AdultsCount { get; set; }
        public int ChildrenCount { get; set; }
        public string SpecialRequests { get; set; }
        public decimal TotalPrice { get; set; }
        public string PaymentStatus { get; set; } // Pending, Confirmed
        public string RoomStatus { get; set; } // CheckedIn, CheckedOut, Cancelled
        
        // Advanced Management Fields
        public string Source { get; set; } // Walk-In, Phone, Web
        public string Notes { get; set; }
        public string BillingType { get; set; } // Individual, Group
        public DateTime LastModified { get; set; }
        public DateTime? CheckInTime { get; set; }
        public DateTime? CheckOutTime { get; set; }
    }
}
