using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace API.Models
{
    public class Driver
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
        public string VehicleNo { get; set; } = null!;
        public string Contact { get; set; } = null!;
        public int TanksInTruck { get; set; }
        public bool IsActive { get; set; }
        public DateTime DateCreated { get; set; } = DateTime.UtcNow;
        // Firestore's SDK refuses to convert a DateTime to a Timestamp unless
        // its Kind is explicitly Utc — the default(DateTime) this would
        // otherwise fall back to is Kind=Unspecified, which throws.
        public DateTime DateUpdated { get; set; } = DateTime.UtcNow;
    }
}