using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;

namespace API.Models
{
    public class AppUser: IdentityUser
    {
        public string Fullname {get; set;} = null!;
        public Guid? ShopId {get; set;}

        // Stored directly on the user's Firestore document as an array field
        // (queried via WhereArrayContains for GetUsersInRoleAsync) rather than
        // a separate join collection — this app only ever assigns a user
        // exactly one role, so a normalized many-to-many join table would be
        // pure overhead here.
        public List<string> Roles { get; set; } = [];
    }
}