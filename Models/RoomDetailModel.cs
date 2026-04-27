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
        // RoomSize is in base RoomModel
        // Description is in base RoomModel

        // Capacity & Bed Configuration
        // MaxOccupancy is in base RoomModel
        // NumberOfBeds is in base RoomModel
        // BedType is in base RoomModel
        // HasExtraBed is in base RoomModel

        // Pricing
        // BasePricePerNight is in base RoomModel
        // ExtraBedPrice is in base RoomModel
        // Currency is in base RoomModel
        // WeekendPrice is in base RoomModel
        // HolidayPrice is in base RoomModel

        // Amenities - stored as flags or comma-separated list
        public List<string> Amenities { get; set; } = new List<string>();

        // Status & Operations
        // AvailabilityStatus is in base RoomModel
        // OperationalStatus is in base RoomModel

        // Housekeeping
        // LastCleanedDate is in base RoomModel
        // HousekeepingNotes is in base RoomModel

        // Maintenance
        // MaintenanceIssue is in base RoomModel
        // MaintenanceDate is in base RoomModel

        // Additional Attributes
        // SmokingAllowed is in base RoomModel
        // WheelchairAccessible is in base RoomModel
        // PetFriendly is in base RoomModel

        // Media
        // ImageUrls is in base RoomModel
        // MainImageUrl is in base RoomModel

        // Notes
        // StaffNotes is in base RoomModel
        // InternalComments is in base RoomModel

        // Timestamps
        // CreatedAt is in base RoomModel
        // UpdatedAt is in base RoomModel
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
