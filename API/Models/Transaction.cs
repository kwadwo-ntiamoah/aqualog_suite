using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace API.Models
{
    public class Transaction
    {
        public Guid Id { get; set; }
        public int Quantity { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal UnitPrice { get; set; }
        public string? Buyer { get; set; }

        public ContainerType Container { get; set; }

        public Guid ShopId { get; set; }
        public string ProcessedByUserId { get; set; } = null!;
        public string PaymentMethod { get; set; } = null!;
        public string? VehicleNo { get; set; }
        public DateTime DateCreated { get; set; } = DateTime.UtcNow;
    }
}