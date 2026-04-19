using System;

namespace HRS.Models
{
    public class PaymentModel
    {
        public string Id { get; set; }
        public string ReservationId { get; set; }
        public decimal Amount { get; set; }
        public DateTime Date { get; set; }
        public string Method { get; set; } // Cash, Credit Card
        public string Status { get; set; } // Pending, Partial, Paid
        public string RecordedByUserId { get; set; }
        public string VerifiedByUserId { get; set; }
        public string Notes { get; set; }
    }
}
