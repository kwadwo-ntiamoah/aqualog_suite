using System;

namespace API.Models
{
    public class Shop
    {
        public Guid Id { get; set; }
        public string DisplayName { get; set; } = null!;
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class ElectricityBalance
    {
        public Guid Id { get; set; }
        public int Balance { get; set; }
        public Guid ShopId {get; set;}
        public string UserId { get; set; } = null!;
        public DateTime DateRecorded { get; set; }
    }
}
