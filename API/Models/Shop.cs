using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace API.Models
{
    public class Shop
    {
        public Guid Id { get; set; }
        public string DisplayName { get; set; } = null!;
        public List<ElectricityBalance> ElectricityBalances { get; set; } = [];
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public ICollection<AppUser> Attendants { get; set; } = [];
    }

    public class ElectricityBalance
    {
        public Guid Id { get; set; }
        public int Balance { get; set; }

        public Guid ShopId {get; set;}
        public Shop? Shop {get; set;}

        public string UserId { get; set; } = null!;
        public AppUser? User { get; set; }
        public DateTime DateRecorded { get; set; }
    }
}