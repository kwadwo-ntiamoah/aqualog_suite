using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace API.Models
{
    public class PasswordResetRequest
    {
        public Guid Id { get; set; }
        public string UserId { get; set; } = null!;
        public AppUser? User { get; set; }
        public bool? IsApproved { get; set; }
        public DateTime RequestedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}