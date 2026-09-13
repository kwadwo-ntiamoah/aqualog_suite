using System;

namespace API.Models
{
    public class AddDriverRequest
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
        public string VehicleNo { get; set; } = null!;
        public string Contact { get; set; } = null!;
        public int TanksInTruck { get; set; }
        public bool IsActive { get; set; }
        public DateTime DateRequested { get; set; } = DateTime.UtcNow;
        // See Driver.DateUpdated — same Firestore Kind=Utc requirement.
        public DateTime DateUpdated { get; set; } = DateTime.UtcNow;
        public string RequestedById {get; set;} = null!;
    }
}
