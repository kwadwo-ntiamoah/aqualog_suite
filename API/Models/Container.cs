using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace API.Models
{
    public enum ContainerType
    {
        TANK = 1,
        BUCKET = 2
    }
    public class Container
    {
        public Guid Id { get; set; }
        public decimal UnitPrice { get; set; }
        public ContainerType Type { get; set; } = ContainerType.TANK;
        public string DisplayName { get; set; } = null!;
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; } = true;
    }
}