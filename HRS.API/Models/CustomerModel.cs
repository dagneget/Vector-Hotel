using System;

namespace HRS.API.Models
{
    public class CustomerModel
    {
        // ── Core ───────────────────────────────────────────────────────────────
        public string Id { get; set; }
        public string FullName { get; set; }
        public string Phone { get; set; }
        public string Email { get; set; }
        public string Gender { get; set; }          // Male, Female, Other, Prefer not to say
        public string Nationality { get; set; }
        public string Address { get; set; }

        // ── Identity ──────────────────────────────────────────────────────────
        public string IdType { get; set; }          // Passport, National ID, Driver's License, Other
        public string IdNumber { get; set; }
        public DateTime? IdExpiryDate { get; set; }
        /// <summary>Legacy field — kept for JSON backwards compatibility. Use IdNumber.</summary>
        public string PassportNumber { get; set; }

        // ── Extended Profile ──────────────────────────────────────────────────
        public DateTime? DateOfBirth { get; set; }
        public string Occupation { get; set; }
        public string Company { get; set; }
        public string EmergencyContactName { get; set; }
        public string EmergencyContactPhone { get; set; }
        public string Notes { get; set; }

        // ── Classification ────────────────────────────────────────────────────
        public string CustomerType { get; set; }    // Regular, VIP, Corporate
        public string Status { get; set; }          // Active, Inactive
        public bool IsBlacklisted { get; set; }
        public string BlacklistReason { get; set; }

        // ── Preferences ───────────────────────────────────────────────────────
        public string PreferredRoomType { get; set; }
        public string SmokingPreference { get; set; }   // Non-Smoking, Smoking, No Preference
        public string FloorPreference { get; set; }     // Low Floor, Mid Floor, High Floor, No Preference
        public string BedTypePreference { get; set; }   // Single, Double, Twin, King, Queen, No Preference

        // ── Loyalty ───────────────────────────────────────────────────────────
        public int LoyaltyPoints { get; set; }
        public string LoyaltyTier { get; set; }     // None, Silver, Gold, Platinum

        // ── Tracking ──────────────────────────────────────────────────────────
        public DateTime? CreatedDate { get; set; }
        public DateTime? LastVisitDate { get; set; }
    }
}
