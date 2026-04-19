namespace HRS.Models
{
    public class RoomModel
    {
        public string Id { get; set; }
        public string RoomNumber { get; set; }
        public string TypeId { get; set; }
        public int FloorNumber { get; set; }
        public string CleanStatus { get; set; } // Clean, Dirty, Maintenance
        public string Status { get; set; } // Available, Reserved, OutOfOrder
    }
}
