using System.Collections.Generic;
using Newtonsoft.Json;

namespace HRS.Models
{
    /// <summary>
    /// Application settings for lookup values - matches backend SystemSettingsModel
    /// </summary>
    public class SettingsModel
    {
        public int Id { get; set; }
        
        // Hotel Identity
        public string HotelName { get; set; } = "Nocturnal Concierge";
        public string HotelAddress { get; set; }
        public string HotelPhone { get; set; }
        public string HotelEmail { get; set; }
        public string LogoData { get; set; }

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

        // Lookup Lists - Stored as JSON strings (matches backend)
        [JsonProperty("currenciesJson")]
        public string CurrenciesJson { get; set; } = "[\"USD\",\"EUR\",\"GBP\",\"JPY\",\"AED\",\"SAR\"]";
        
        [JsonProperty("bedTypesJson")]
        public string BedTypesJson { get; set; } = "[\"Single\",\"Twin\",\"Queen\",\"King\"]";

        // Helper properties (not serialized to JSON)
        [JsonIgnore]
        public List<string> Currencies 
        { 
            get => JsonConvert.DeserializeObject<List<string>>(CurrenciesJson ?? "[]");
            set => CurrenciesJson = JsonConvert.SerializeObject(value);
        }

        [JsonIgnore]
        public List<string> BedTypes 
        { 
            get => JsonConvert.DeserializeObject<List<string>>(BedTypesJson ?? "[]");
            set => BedTypesJson = JsonConvert.SerializeObject(value);
        }
    }
}
