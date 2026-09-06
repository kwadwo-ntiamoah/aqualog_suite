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
        public Shop? Shop {get; set;}
    }
}