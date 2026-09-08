using Google.Cloud.Firestore;
using Microsoft.AspNetCore.Identity;

namespace API.Infrastructure.Identity
{
    public class FirestoreRoleStore(FirestoreDb db) : IRoleStore<IdentityRole>
    {
        private CollectionReference Roles => db.Collection("roles");

        public void Dispose() { }

        public async Task<IdentityResult> CreateAsync(IdentityRole role, CancellationToken ct)
        {
            await Roles.Document(role.Id).SetAsync(ToDocument(role), cancellationToken: ct);
            return IdentityResult.Success;
        }

        public async Task<IdentityResult> UpdateAsync(IdentityRole role, CancellationToken ct)
        {
            await Roles.Document(role.Id).SetAsync(ToDocument(role), cancellationToken: ct);
            return IdentityResult.Success;
        }

        public async Task<IdentityResult> DeleteAsync(IdentityRole role, CancellationToken ct)
        {
            await Roles.Document(role.Id).DeleteAsync(cancellationToken: ct);
            return IdentityResult.Success;
        }

        public async Task<IdentityRole?> FindByIdAsync(string roleId, CancellationToken ct)
        {
            var snapshot = await Roles.Document(roleId).GetSnapshotAsync(ct);
            return snapshot.Exists ? FromDocument(snapshot) : null;
        }

        public async Task<IdentityRole?> FindByNameAsync(string normalizedRoleName, CancellationToken ct)
        {
            var query = await Roles.WhereEqualTo("NormalizedName", normalizedRoleName).Limit(1).GetSnapshotAsync(ct);
            var snapshot = query.Documents.FirstOrDefault();
            return snapshot is not null ? FromDocument(snapshot) : null;
        }

        public Task<string> GetRoleIdAsync(IdentityRole role, CancellationToken ct) => Task.FromResult(role.Id);
        public Task<string?> GetRoleNameAsync(IdentityRole role, CancellationToken ct) => Task.FromResult(role.Name);
        public Task SetRoleNameAsync(IdentityRole role, string? roleName, CancellationToken ct)
        {
            role.Name = roleName;
            return Task.CompletedTask;
        }

        public Task<string?> GetNormalizedRoleNameAsync(IdentityRole role, CancellationToken ct) => Task.FromResult(role.NormalizedName);
        public Task SetNormalizedRoleNameAsync(IdentityRole role, string? normalizedName, CancellationToken ct)
        {
            role.NormalizedName = normalizedName;
            return Task.CompletedTask;
        }

        private static IdentityRole FromDocument(DocumentSnapshot snapshot) => new()
        {
            Id = snapshot.Id,
            Name = snapshot.GetValue<string?>("Name"),
            NormalizedName = snapshot.GetValue<string?>("NormalizedName"),
        };

        private static Dictionary<string, object?> ToDocument(IdentityRole role) => new()
        {
            ["Name"] = role.Name,
            ["NormalizedName"] = role.NormalizedName,
        };
    }
}
