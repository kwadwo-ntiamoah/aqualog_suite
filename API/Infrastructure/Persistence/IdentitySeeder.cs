using API.Models;
using Microsoft.AspNetCore.Identity;

namespace API.Infrastructure.Persistence
{
    public class IdentitySeeder
    {
        public static async Task CreateDefaultUsers(UserManager<AppUser> _userManager, RoleManager<IdentityRole> _roleManager, IConfiguration _config)
        {
            var roles = new List<string> { "attendant", "admin" };

            await CreateRolesAsync(_roleManager, roles);

            var defaultUsers = _config.GetSection("SeedUsers").Get<List<SeedUser>>() ?? [];

            foreach (var user in defaultUsers)
            {
                await CreateOrUpdateUserAsync(_userManager, user);
            }
        }

        private static async Task CreateRolesAsync(RoleManager<IdentityRole> _roleManager, IEnumerable<string> roles)
        {
            foreach (var role in roles)
            {
                if (!await _roleManager.RoleExistsAsync(role))
                {
                    var identityRole = new IdentityRole { Name = role };
                    var result = await _roleManager.CreateAsync(identityRole);

                    if (!result.Succeeded)
                    {
                        string errorMessage = string.Join(".", result.Errors.Select(x => x.Description));
                        throw new InvalidOperationException(errorMessage);
                    }
                }
            }
        }

        private static async Task CreateOrUpdateUserAsync(UserManager<AppUser> _userManager, SeedUser user)
        {
            if (await _userManager.FindByNameAsync(user.UserName) == null)
            {
                var defaultUser = new AppUser
                {
                    UserName = user.UserName,
                    Fullname = user.FullName
                };

                IdentityResult result;

                if (string.IsNullOrEmpty(user.Password))
                {
                    result = await _userManager.CreateAsync(defaultUser);
                }
                else
                {
                    result = await _userManager.CreateAsync(defaultUser, user.Password);
                }

                if (!result.Succeeded)
                {
                    string errorMessage = string.Join(".", result.Errors.Select(x => x.Description));
                    throw new InvalidOperationException(errorMessage);
                }

                await AddUserToRolesAsync(_userManager, defaultUser, user.Roles);
            }
        }

        private static async Task AddUserToRolesAsync(UserManager<AppUser> _userManager, AppUser user, IEnumerable<string> roles)
        {
            if (roles?.Any() == true)
            {
                var result = await _userManager.AddToRolesAsync(user, roles);

                if (!result.Succeeded)
                {
                    string errorMessage = string.Join(".", result.Errors.Select(x => x.Description));
                    throw new InvalidOperationException(errorMessage);
                }
            }
        }

        public class SeedUser
        {
            public string UserName { get; set; } = string.Empty;
            public string FullName {get; set;} = string.Empty;
            public string Password { get; set; } = string.Empty;
            public List<string> Roles { get; set; } = [];
        }
    }
}
