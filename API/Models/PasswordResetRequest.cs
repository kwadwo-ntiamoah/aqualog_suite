using System;

namespace API.Models
{
    public class PasswordResetRequest
    {
        public Guid Id { get; set; }
        public string UserId { get; set; } = null!;
        public bool? IsApproved { get; set; }
        public DateTime RequestedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
