using System.ComponentModel.DataAnnotations.Schema;

namespace HRS.API.Models
{
    public class RoomModel
    {
        // Basic Information
        public string Id { get; set; }
        public string RoomNumber { get; set; }
        public string TypeId { get; set; }
        public int FloorNumber { get; set; }
        public decimal RoomSize { get; set; } // in square meters
        public string Description { get; set; }
        
        // Status
        public string CleanStatus { get; set; } // Clean, Dirty, Maintenance
        public string Status { get; set; } // Available, Reserved, OutOfOrder
        public string AvailabilityStatus { get; set; } = "Available"; // Available, Occupied, Reserved, OutOfService
        public string OperationalStatus { get; set; } = "Normal"; // Normal, MaintenanceRequired
        
        // Capacity & Bed Configuration
        public int MaxOccupancy { get; set; }
        public int NumberOfBeds { get; set; }
        public string BedType { get; set; } // Single, Twin, Queen, King
        public bool HasExtraBed { get; set; }
        public decimal ExtraBedPrice { get; set; }
        
        // Pricing
        public decimal BasePricePerNight { get; set; }
        public decimal WeekendPrice { get; set; }
        public decimal HolidayPrice { get; set; }
        public string Currency { get; set; } = "USD";
        
        // Amenities - stored as JSON string
        public string AmenitiesJson { get; set; }
        
        // Images - stored as JSON string
        public string ImageUrlsJson { get; set; }
        public string MainImageUrl { get; set; }
        
        // Housekeeping
        public DateTime? LastCleanedDate { get; set; }
        public string HousekeepingNotes { get; set; }
        
        // Maintenance
        public string MaintenanceIssue { get; set; }
        public DateTime? MaintenanceDate { get; set; }
        
        // Additional Attributes
        public bool SmokingAllowed { get; set; }
        public bool WheelchairAccessible { get; set; }
        public bool PetFriendly { get; set; }
        
        // Notes
        public string StaffNotes { get; set; }
        public string InternalComments { get; set; }
        
        // Timestamps
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}
