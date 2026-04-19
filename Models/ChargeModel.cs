using System;

namespace HRS.Models
{
    public class ChargeModel
    {
        public string Id { get; set; }
        public string ReservationId { get; set; }
        public string Description { get; set; }
        public decimal Amount { get; set; }
        public DateTime Date { get; set; }
    }
}
