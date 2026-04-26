namespace HRS.Models
{
    public class SystemSettingsModel
    {
        public int Id { get; set; } = 1;

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
    }
}
