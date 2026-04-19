using System;

namespace HRS.Models
{
    public class AuditLogModel
    {
        public string Id { get; set; }
        public string UserId { get; set; }
        public string UserRole { get; set; }
        public string Action { get; set; }
        public string Details { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
