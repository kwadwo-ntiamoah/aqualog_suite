using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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
        public DateTime DateUpdated { get; set; }

        public string RequestedById {get; set;} = null!;
        public AppUser? RequestedBy {get; set;} 
    }
}