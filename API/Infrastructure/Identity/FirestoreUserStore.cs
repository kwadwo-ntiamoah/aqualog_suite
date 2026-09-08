using API.Models;
using Google.Cloud.Firestore;
using Microsoft.AspNetCore.Identity;

namespace API.Infrastructure.Identity
{
    // ASP.NET Identity's UserManager is store-agnostic — it always mutates the
    // in-memory `user` object first (Set*/AddToRole/etc.) and then calls
    // CreateAsync or UpdateAsync to persist the result, exactly once per
    // logical operation (this is the same pattern EF Core's own UserStore
    // follows). So every "Set"/"Add"/"Remove" method here just stages state
    // on `user` in memory; only CreateAsync/UpdateAsync actually touch
    // Firestore. Only the interfaces this app's UserManager usage actually
    // exercises are implemented (no email/claims/lockout/two-factor stores —
    // nothing in AuthService calls SignInManager or those UserManager members).
    public class FirestoreUserStore(FirestoreDb db) :
        IUserStore<AppUser>,
        IUserPasswordStore<AppUser>,
        IUserRoleStore<AppUser>,
        IUserSecurityStampStore<AppUser>
    {
        private CollectionReference Users => db.Collection("users");

        public void Dispose() { }

        public async Task<IdentityResult> CreateAsync(AppUser user, CancellationToken ct)
        {
            await Users.Document(user.Id).SetAsync(ToDocument(user), cancellationToken: ct);
            return IdentityResult.Success;
        }

        public async Task<IdentityResult> UpdateAsync(AppUser user, CancellationToken ct)
        {
            await Users.Document(user.Id).SetAsync(ToDocument(user), cancellationToken: ct);
            return IdentityResult.Success;
        }

        public async Task<IdentityResult> DeleteAsync(AppUser user, CancellationToken ct)
        {
            await Users.Document(user.Id).DeleteAsync(cancellationToken: ct);
            return IdentityResult.Success;
        }

        public async Task<AppUser?> FindByIdAsync(string userId, CancellationToken ct)
        {
            var snapshot = await Users.Document(userId).GetSnapshotAsync(ct);
            return snapshot.Exists ? FromDocument(snapshot) : null;
        }

        public async Task<AppUser?> FindByNameAsync(string normalizedUserName, CancellationToken ct)
        {
            var query = await Users.WhereEqualTo("NormalizedUserName", normalizedUserName).Limit(1).GetSnapshotAsync(ct);
            var snapshot = query.Documents.FirstOrDefault();
            return snapshot is not null ? FromDocument(snapshot) : null;
        }

        public Task<string> GetUserIdAsync(AppUser user, CancellationToken ct) => Task.FromResult(user.Id);
        public Task<string?> GetUserNameAsync(AppUser user, CancellationToken ct) => Task.FromResult(user.UserName);
        public Task SetUserNameAsync(AppUser user, string? userName, CancellationToken ct)
        {
            user.UserName = userName;
            return Task.CompletedTask;
        }

        public Task<string?> GetNormalizedUserNameAsync(AppUser user, CancellationToken ct) => Task.FromResult(user.NormalizedUserName);
        public Task SetNormalizedUserNameAsync(AppUser user, string? normalizedName, CancellationToken ct)
        {
            user.NormalizedUserName = normalizedName;
            return Task.CompletedTask;
        }

        public Task SetPasswordHashAsync(AppUser user, string? passwordHash, CancellationToken ct)
        {
            user.PasswordHash = passwordHash;
            return Task.CompletedTask;
        }

        public Task<string?> GetPasswordHashAsync(AppUser user, CancellationToken ct) => Task.FromResult(user.PasswordHash);
        public Task<bool> HasPasswordAsync(AppUser user, CancellationToken ct) => Task.FromResult(!string.IsNullOrEmpty(user.PasswordHash));

        public Task SetSecurityStampAsync(AppUser user, string stamp, CancellationToken ct)
        {
            user.SecurityStamp = stamp;
            return Task.CompletedTask;
        }

        public Task<string?> GetSecurityStampAsync(AppUser user, CancellationToken ct) => Task.FromResult(user.SecurityStamp);

        public Task AddToRoleAsync(AppUser user, string normalizedRoleName, CancellationToken ct)
        {
            if (!user.Roles.Contains(normalizedRoleName)) user.Roles.Add(normalizedRoleName);
            return Task.CompletedTask;
        }

        public Task RemoveFromRoleAsync(AppUser user, string normalizedRoleName, CancellationToken ct)
        {
            user.Roles.Remove(normalizedRoleName);
            return Task.CompletedTask;
        }

        public Task<IList<string>> GetRolesAsync(AppUser user, CancellationToken ct) => Task.FromResult<IList<string>>(user.Roles);
        public Task<bool> IsInRoleAsync(AppUser user, string normalizedRoleName, CancellationToken ct) =>
            Task.FromResult(user.Roles.Contains(normalizedRoleName));

        public async Task<IList<AppUser>> GetUsersInRoleAsync(string normalizedRoleName, CancellationToken ct)
        {
            var query = await Users.WhereArrayContains("Roles", normalizedRoleName).GetSnapshotAsync(ct);
            return query.Documents.Select(FromDocument).ToList();
        }

        public static AppUser FromDocument(DocumentSnapshot snapshot)
        {
            return new AppUser
            {
                Id = snapshot.Id,
                UserName = snapshot.GetValue<string?>("UserName"),
                NormalizedUserName = snapshot.GetValue<string?>("NormalizedUserName"),
                Fullname = snapshot.GetValue<string?>("Fullname") ?? "",
                ShopId = snapshot.TryGetValue<string?>("ShopId", out var shopId) && shopId is not null ? Guid.Parse(shopId) : null,
                PasswordHash = snapshot.GetValue<string?>("PasswordHash"),
                SecurityStamp = snapshot.GetValue<string?>("SecurityStamp"),
                Roles = snapshot.TryGetValue<List<string>?>("Roles", out var roles) && roles is not null ? roles : [],
            };
        }

        private static Dictionary<string, object?> ToDocument(AppUser user) => new()
        {
            ["UserName"] = user.UserName,
            ["NormalizedUserName"] = user.NormalizedUserName,
            ["Fullname"] = user.Fullname,
            ["ShopId"] = user.ShopId?.ToString(),
            ["PasswordHash"] = user.PasswordHash,
            ["SecurityStamp"] = user.SecurityStamp,
            ["Roles"] = user.Roles,
        };
    }
}
