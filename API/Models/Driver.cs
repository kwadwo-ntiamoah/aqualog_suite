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
        public DateTime DateUpdated { get; set; }
    }
}