using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace HRS.API.Models
{
    public class SystemSettingsModel
    {
        [Key]
        public int Id { get; set; }

        // Hotel Identity
        [Required]
        public string HotelName { get; set; } = "Nocturnal Concierge";
        public string HotelAddress { get; set; }
        public string HotelPhone { get; set; }
        public string HotelEmail { get; set; }
        public string LogoData { get; set; } // Base64 image data

        // Financial Settings
        public string DefaultCurrency { get; set; } = "USD";
        public decimal TaxRate { get; set; } = 0;
        public bool AllowPriceOverride { get; set; } = true;

        // Reservation & Payment Policies
        public bool RequireFullPaymentBeforeCheckIn { get; set; } = false;
        public bool AllowPartialPayments { get; set; } = true;
        public string DefaultReservationStatus { get; set; } = "Pending";
        public bool AllowReservationCancellation { get; set; } = true;

        // System Preferences
        public string DateFormat { get; set; } = "MMM dd, yyyy";
        public string TimeFormat { get; set; } = "hh:mm tt";
        public string Theme { get; set; } = "Dark";

        // Lookup Lists - Stored as JSON strings
        public string CurrenciesJson { get; set; } = "[\"USD\",\"EUR\",\"GBP\",\"JPY\",\"AED\",\"SAR\"]";
        public string BedTypesJson { get; set; } = "[\"Single\",\"Twin\",\"Queen\",\"King\"]";

        // Helper methods to get/set lists
        [System.Text.Json.Serialization.JsonIgnore]
        public List<string> Currencies 
        { 
            get => System.Text.Json.JsonSerializer.Deserialize<List<string>>(CurrenciesJson ?? "[]");
            set => CurrenciesJson = System.Text.Json.JsonSerializer.Serialize(value);
        }

        [System.Text.Json.Serialization.JsonIgnore]
        public List<string> BedTypes 
        { 
            get => System.Text.Json.JsonSerializer.Deserialize<List<string>>(BedTypesJson ?? "[]");
            set => BedTypesJson = System.Text.Json.JsonSerializer.Serialize(value);
        }
    }
}
