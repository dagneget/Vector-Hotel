using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using HRS.Models;

namespace HRS.Services
{
    public class CustomerStats
    {
        public int TotalStays { get; set; }
        public int TotalNightsStayed { get; set; }
        public decimal TotalSpent { get; set; }
        public DateTime? LastVisitDate { get; set; }
        public DateTime? LastReservationDate { get; set; }
        public DateTime? LastPaymentDate { get; set; }
        public string MostUsedRoomType { get; set; }
        public double AverageStayDuration { get; set; }
    }

    public static class CustomerService
    {
        // ── Validation ────────────────────────────────────────────────────────

        public static List<string> ValidateCustomer(CustomerModel c, string editingId = null)
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(c.FullName))
                errors.Add("Full Name is required.");

            if (string.IsNullOrWhiteSpace(c.Phone))
                errors.Add("Phone Number is required.");
            else if (!Regex.IsMatch(c.Phone.Trim(), @"^[+\d][\d\s\-\(\)]{5,19}$"))
                errors.Add("Phone format is invalid. Use digits, spaces, dashes (e.g. +1 555-1234).");

            if (!string.IsNullOrWhiteSpace(c.Email) && !IsValidEmail(c.Email))
                errors.Add("Email address format is invalid.");

            if (c.IsBlacklisted && string.IsNullOrWhiteSpace(c.BlacklistReason))
                errors.Add("A reason is required when blacklisting a customer.");

            // Uniqueness checks
            foreach (var existing in DataStore.Data.Customers)
            {
                if (existing.Id == editingId) continue;

                if (!string.IsNullOrWhiteSpace(c.Phone) &&
                    !string.IsNullOrWhiteSpace(existing.Phone) &&
                    existing.Phone.Trim() == c.Phone.Trim())
                    errors.Add($"Phone number is already registered to '{existing.FullName}'.");

                if (!string.IsNullOrWhiteSpace(c.Email) &&
                    !string.IsNullOrWhiteSpace(existing.Email) &&
                    existing.Email.Trim().ToLower() == c.Email.Trim().ToLower())
                    errors.Add($"Email is already registered to '{existing.FullName}'.");

                if (!string.IsNullOrWhiteSpace(c.IdNumber) &&
                    !string.IsNullOrWhiteSpace(existing.IdNumber) &&
                    existing.IdNumber.Trim().ToLower() == c.IdNumber.Trim().ToLower())
                    errors.Add($"ID Number is already registered to '{existing.FullName}'.");
            }

            return errors;
        }

        // ── Duplicate Detection ───────────────────────────────────────────────

        public static List<CustomerModel> CheckDuplicates(CustomerModel c, string editingId = null)
        {
            return DataStore.Data.Customers.Where(existing =>
            {
                if (existing.Id == editingId) return false;

                bool phoneMatch = !string.IsNullOrWhiteSpace(c.Phone) &&
                                  !string.IsNullOrWhiteSpace(existing.Phone) &&
                                  existing.Phone.Trim() == c.Phone.Trim();

                bool emailMatch = !string.IsNullOrWhiteSpace(c.Email) &&
                                  !string.IsNullOrWhiteSpace(existing.Email) &&
                                  existing.Email.Trim().ToLower() == c.Email.Trim().ToLower();

                bool idMatch = !string.IsNullOrWhiteSpace(c.IdNumber) &&
                               !string.IsNullOrWhiteSpace(existing.IdNumber) &&
                               existing.IdNumber.Trim().ToLower() == c.IdNumber.Trim().ToLower();

                return phoneMatch || emailMatch || idMatch;
            }).ToList();
        }

        // ── Delete Guard ──────────────────────────────────────────────────────

        public static (bool CanDelete, string Reason) CanDelete(string customerId)
        {
            bool hasActive = DataStore.Data.Reservations.Any(r =>
                r.CustomerId == customerId &&
                (r.Status == "Pending" || r.Status == "Confirmed" || r.Status == "CheckedIn"));

            return hasActive
                ? (false, "Cannot delete: this customer has active reservations.")
                : (true, null);
        }

        // ── Statistics ────────────────────────────────────────────────────────

        public static CustomerStats GetStats(string customerId)
        {
            var allReservations = DataStore.Data.Reservations
                .Where(r => r.CustomerId == customerId).ToList();

            var completed = allReservations
                .Where(r => r.Status == "CheckedOut").ToList();

            int totalNights = completed.Sum(r =>
                Math.Max(1, (int)(r.CheckOut.Date - r.CheckIn.Date).TotalDays));

            var payments = GetPayments(customerId);
            decimal totalSpent = payments.Sum(p => p.Amount);

            DateTime? lastVisit = completed.Any()
                ? (DateTime?)completed.Max(r => r.CheckOut)
                : null;

            DateTime? lastRes = allReservations.Any()
                ? (DateTime?)allReservations.Max(r => r.CheckIn)
                : null;

            DateTime? lastPay = payments.Any()
                ? (DateTime?)payments.Max(p => p.Date)
                : null;

            // Most-used room type
            string mostUsed = "—";
            if (allReservations.Any())
            {
                var grouped = allReservations
                    .Join(DataStore.Data.Rooms, r => r.RoomId, rm => rm.Id, (r, rm) => rm.TypeId)
                    .Join(DataStore.Data.RoomTypes, tid => tid, t => t.Id, (_, t) => t.Name)
                    .GroupBy(name => name)
                    .OrderByDescending(g => g.Count())
                    .FirstOrDefault();
                if (grouped != null) mostUsed = grouped.Key;
            }

            double avgDuration = completed.Any()
                ? completed.Average(r => Math.Max(1.0, (r.CheckOut.Date - r.CheckIn.Date).TotalDays))
                : 0;

            return new CustomerStats
            {
                TotalStays = completed.Count,
                TotalNightsStayed = totalNights,
                TotalSpent = totalSpent,
                LastVisitDate = lastVisit,
                LastReservationDate = lastRes,
                LastPaymentDate = lastPay,
                MostUsedRoomType = mostUsed,
                AverageStayDuration = avgDuration
            };
        }

        public static List<ReservationModel> GetReservations(string customerId) =>
            DataStore.Data.Reservations
                .Where(r => r.CustomerId == customerId)
                .OrderByDescending(r => r.CheckIn)
                .ToList();

        public static List<PaymentModel> GetPayments(string customerId) =>
            DataStore.Data.Payments
                .Join(DataStore.Data.Reservations,
                    p => p.ReservationId, r => r.Id,
                    (p, r) => new { p, r })
                .Where(x => x.r.CustomerId == customerId)
                .Select(x => x.p)
                .OrderByDescending(p => p.Date)
                .ToList();

        // ── Loyalty ───────────────────────────────────────────────────────────

        public static string CalculateLoyaltyTier(int totalStays, decimal totalSpent)
        {
            if (totalStays >= 15 || totalSpent >= 3000) return "Platinum";
            if (totalStays >= 8  || totalSpent >= 1500) return "Gold";
            if (totalStays >= 3  || totalSpent >= 500)  return "Silver";
            return "None";
        }

        /// <summary>
        /// Recalculates and updates loyalty points + tier in place on the model.
        /// Caller is responsible for calling DataStore.Save() afterwards.
        /// </summary>
        public static void UpdateLoyalty(CustomerModel customer)
        {
            if (string.IsNullOrEmpty(customer.Id)) return;
            var stats = GetStats(customer.Id);
            customer.LoyaltyPoints = (stats.TotalStays * 10) + (int)(stats.TotalSpent / 10);
            customer.LoyaltyTier   = CalculateLoyaltyTier(stats.TotalStays, stats.TotalSpent);
            customer.LastVisitDate = stats.LastVisitDate;
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private static bool IsValidEmail(string email)
        {
            int at = email.IndexOf('@');
            if (at <= 0) return false;
            string domain = email.Substring(at + 1);
            return domain.Contains('.');
        }
    }
}
