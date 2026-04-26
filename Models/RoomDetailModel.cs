using System;
using System.Collections.Generic;

namespace HRS.Models
{
    /// <summary>
    /// Extended room model with comprehensive details for the Add/Edit Room form
    /// </summary>
    public class RoomDetailModel : RoomModel
    {
        // Basic Information
        public string RoomName { get; set; }
        public decimal RoomSize { get; set; } // in square meters
        public string Description { get; set; }

        // Capacity & Bed Configuration
        public int MaxOccupancy { get; set; }
        public int NumberOfBeds { get; set; }
        public string BedType { get; set; } // Single, Twin, Queen, King
        public bool HasExtraBed { get; set; }

        // Pricing
        public decimal BasePricePerNight { get; set; }
        public decimal ExtraBedPrice { get; set; }
        public string Currency { get; set; } = "USD";
        public decimal WeekendPrice { get; set; }
        public decimal HolidayPrice { get; set; }

        // Amenities - stored as flags or comma-separated list
        public List<string> Amenities { get; set; } = new List<string>();

        // Status & Operations
        public string AvailabilityStatus { get; set; } = "Available"; // Available, Occupied, Reserved, OutOfService
        public string OperationalStatus { get; set; } = "Normal"; // Normal, MaintenanceRequired

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

        // Media
        public List<string> ImageUrls { get; set; } = new List<string>();
        public string MainImageUrl { get; set; }

        // Notes
        public string StaffNotes { get; set; }
        public string InternalComments { get; set; }

        // Timestamps
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }
    }

    /// <summary>
    /// Amenity categories and options for checkbox binding
    /// </summary>
    public static class RoomAmenities
    {
        public static readonly Dictionary<string, List<string>> Categories = new Dictionary<string, List<string>>
        {
            ["Basic"] = new List<string> { "Wi-Fi", "Air Conditioning", "TV", "Telephone" },
            ["Bathroom"] = new List<string> { "Private Bathroom", "Shower", "Bathtub", "Hot Water" },
            ["Furniture"] = new List<string> { "Desk", "Chair", "Wardrobe", "Mirror" },
            ["Extras"] = new List<string> { "Mini Bar", "Refrigerator", "Coffee/Tea Maker", "Safe Box" },
            ["Special"] = new List<string> { "Balcony", "Sea View", "City View", "Garden View" }
        };

        public static readonly List<string> AllAmenities = new List<string>
        {
            "Wi-Fi", "Air Conditioning", "TV", "Telephone",
            "Private Bathroom", "Shower", "Bathtub", "Hot Water",
            "Desk", "Chair", "Wardrobe", "Mirror",
            "Mini Bar", "Refrigerator", "Coffee/Tea Maker", "Safe Box",
            "Balcony", "Sea View", "City View", "Garden View"
        };
    }

    public static class RoomStatuses
    {
        public static readonly List<string> AvailabilityStatuses = new List<string>
        {
            "Available", "Occupied", "Reserved", "Out of Service"
        };

        public static readonly List<string> CleaningStatuses = new List<string>
        {
            "Clean", "Dirty", "In Progress"
        };

        public static readonly List<string> MaintenanceStatuses = new List<string>
        {
            "Normal", "Maintenance Required"
        };

        public static List<string> BedTypes { get; set; } = new List<string>
        {
            "Single", "Twin", "Queen", "King"
        };

        public static List<string> Currencies { get; set; } = new List<string>
        {
            "USD", "EUR", "GBP", "JPY", "AED", "SAR"
        };
        
        // Call this after DataStore.LoadAsync() to sync with database
        public static void SyncWithDataStore()
        {
            if (HRS.Services.DataStore.Data?.Currencies?.Count > 0)
                Currencies = new List<string>(HRS.Services.DataStore.Data.Currencies);
            if (HRS.Services.DataStore.Data?.BedTypes?.Count > 0)
                BedTypes = new List<string>(HRS.Services.DataStore.Data.BedTypes);
        }
    }
}
