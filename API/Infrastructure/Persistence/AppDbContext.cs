using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using API.Models;
using Microsoft.EntityFrameworkCore;

namespace API.Infrastructure.Persistence
{
    public class AppDbContext(DbContextOptions options) : IdentityDbContext<AppUser>(options)
    {
        public DbSet<AddDriverRequest> AddDriverRequests { get; set; }
        public DbSet<Container> Containers { get; set; }
        public DbSet<Driver> Drivers { get; set; }
        public DbSet<ElectricityBalance> ElectricityBalances {get; set;}
        public DbSet<PasswordResetRequest> PasswordResetRequests { get; set; }
        public DbSet<Shop> Shops { get; set; }
        public DbSet<Transaction> Transactions { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
        }
    }
}