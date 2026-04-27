using System;
using System.Text.RegularExpressions;
using HRS.ViewModels;

namespace HRS.Models
{
    public class CustomerModel : ViewModelBase
    {
        // ── Core ───────────────────────────────────────────────────────────────
        private string _id;
        public string Id { get => _id; set => SetProperty(ref _id, value); }
        
        private string _fullName;
        public string FullName { get => _fullName; set => SetProperty(ref _fullName, value); }
        
        private string _phone;
        public string Phone { get => _phone; set => SetProperty(ref _phone, value); }
        
        private string _email;
        public string Email { get => _email; set => SetProperty(ref _email, value); }
        
        private string _gender;
        public string Gender { get => _gender; set => SetProperty(ref _gender, value); }
        
        private string _nationality;
        public string Nationality { get => _nationality; set => SetProperty(ref _nationality, value); }
        
        private string _address;
        public string Address { get => _address; set => SetProperty(ref _address, value); }

        // ... continue with other properties if needed, but these are the main ones for validation ...

        // ── Identity ──────────────────────────────────────────────────────────
        private string _idType;
        public string IdType { get => _idType; set => SetProperty(ref _idType, value); }
        
        private string _idNumber;
        public string IdNumber { get => _idNumber; set => SetProperty(ref _idNumber, value); }
        
        private DateTime? _idExpiryDate;
        public DateTime? IdExpiryDate { get => _idExpiryDate; set => SetProperty(ref _idExpiryDate, value); }
        
        public string PassportNumber { get; set; }

        // ── Extended Profile ──────────────────────────────────────────────────
        public DateTime? DateOfBirth { get; set; }
        public string Occupation { get; set; }
        public string Company { get; set; }
        public string EmergencyContactName { get; set; }
        public string EmergencyContactPhone { get; set; }
        public string Notes { get; set; }

        // ── Classification ────────────────────────────────────────────────────
        private string _customerType;
        public string CustomerType { get => _customerType; set => SetProperty(ref _customerType, value); }
        
        private string _status;
        public string Status { get => _status; set => SetProperty(ref _status, value); }
        
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

        // ── Validation Logic ──────────────────────────────────────────────────
        protected override void ValidateProperty(string propertyName)
        {
            RemoveError(propertyName);

            switch (propertyName)
            {
                case nameof(FullName):
                    if (string.IsNullOrWhiteSpace(FullName))
                        AddError(propertyName, "Full Name is required.");
                    else if (FullName.Length < 3)
                        AddError(propertyName, "Name is too short.");
                    break;

                case nameof(Phone):
                    if (string.IsNullOrWhiteSpace(Phone))
                        AddError(propertyName, "Phone Number is required.");
                    else if (!Regex.IsMatch(Phone, @"^\+?[0-9]{7,15}$"))
                        AddError(propertyName, "Invalid phone format (7-15 digits).");
                    break;

                case nameof(Email):
                    if (!string.IsNullOrWhiteSpace(Email))
                    {
                        if (!Regex.IsMatch(Email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
                            AddError(propertyName, "Invalid email format.");
                    }
                    break;

                case nameof(IdNumber):
                    if (string.IsNullOrWhiteSpace(IdNumber))
                        AddError(propertyName, "ID Number is required.");
                    break;

                case nameof(IdType):
                    if (string.IsNullOrWhiteSpace(IdType))
                        AddError(propertyName, "ID Type is required.");
                    break;
            }
        }
    }
}
