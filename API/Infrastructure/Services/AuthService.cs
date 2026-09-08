using API.Infrastructure.Identity;
using API.Models;
using ErrorOr;
using Google.Cloud.Firestore;
using Microsoft.AspNetCore.Identity;
using Newtonsoft.Json;
using static API.Infrastructure.Services.Common;

namespace API.Infrastructure.Services
{
    public class AuthService(UserManager<AppUser> userManager, TokenService tokenService, FirestoreDb db)
    {
        private CollectionReference Users => db.Collection("users");
        private CollectionReference Shops => db.Collection("shops");
        private CollectionReference PasswordResetRequests => db.Collection("passwordResetRequests");

        public async Task<ErrorOr<Token>> GetTokenAsync(string username, string password)
        {
            try
            {
                var user = await userManager.FindByNameAsync(username);
                if (user != null && (await userManager.CheckPasswordAsync(user, password)))
                {
                    var roles = await userManager.GetRolesAsync(user);
                    var role = roles.FirstOrDefault() ?? "attendant";

                    var token = tokenService.GenerateToken(user.Id, username, role);

                    return new Token
                    {
                        AccessToken = token,
                        ExpiresIn = 0
                    };
                }

                return Error.Unauthorized(description: "Invalid login credentials");
            }
            catch (Exception ex)
            {
                return Error.Failure(description: ex.Message);
            }
        }

        public async Task<ErrorOr<MeResponse>> GetMeAsync(string userId)
        {
            try
            {
                var user = await userManager.FindByIdAsync(userId);
                if (user is null) return Error.Unauthorized(description: "Not authorized to perform this action");

                var shopName = await GetShopNameAsync(user.ShopId);
                var roles = await userManager.GetRolesAsync(user);

                return new MeResponse
                {
                    UserId = user.Id,
                    Username = user.UserName!,
                    Fullname = user.Fullname,
                    ShopId = user.ShopId,
                    ShopName = shopName,
                    Role = roles.FirstOrDefault() ?? "attendant"
                };
            }
            catch (Exception ex)
            {
                return Error.Failure(description: ex.Message);
            }
        }

        public async Task<ErrorOr<SuccessResponse>> ResetPasswordAsync(string userId)
        {
            try
            {
                var user = await userManager.FindByIdAsync(userId);
                if (user == null) return Error.Unauthorized(description: "Not authorized to perform this action");

                var resetPasswordToken = await userManager.GeneratePasswordResetTokenAsync(user);
                var response = await userManager.ResetPasswordAsync(user, resetPasswordToken, "P@ssword12345");

                if (response.Succeeded) return new SuccessResponse { Message = "Password reset completed SuccessResponsefully" };
                return Error.Failure(description: "An error occurred changing password. Try again later");
            }
            catch (Exception ex)
            {
                return Error.Failure(description: ex.Message);
            }
        }

        public async Task<ErrorOr<SuccessResponse>> ChangePasswordAsync(string userId, string oldPassword, string newPassword)
        {
            try
            {
                var user = await userManager.FindByIdAsync(userId);
                if (user == null) return Error.Unauthorized(description: "Not authorized to perform this action");

                var response = await userManager.ChangePasswordAsync(user, oldPassword, newPassword);

                if (response.Succeeded) return new SuccessResponse { Message = "Password changed SuccessResponsefully" };
                return Error.Failure(description: "An error occurred changing password. Try again later");
            }
            catch (Exception ex)
            {
                return Error.Failure(description: ex.Message);
            }
        }

        public async Task<ErrorOr<SuccessResponse>> AddUserAsync(string username, string fullname, string tempPassword, Guid shopId)
        {
            try
            {
                var user = await userManager.FindByNameAsync(username);
                if (user != null) return Error.Conflict(description: "User already exists");

                var newUser = new AppUser
                {
                    UserName = username,
                    Fullname = fullname,
                    ShopId = shopId
                };

                var addUserResponse = await userManager.CreateAsync(newUser, tempPassword);
                if (addUserResponse.Succeeded) return new SuccessResponse { Message = "User added SuccessResponsefully" };

                return Error.Failure(description: "Error occurred adding user to shop");

            } catch(Exception ex)
            {
                return Error.Failure(description: ex.Message);
            }
        }

        public async Task<ErrorOr<List<UserSummaryDto>>> GetUsersAsync()
        {
            try
            {
                var snapshot = await Users.GetSnapshotAsync();
                var users = snapshot.Documents.Select(FirestoreUserStore.FromDocument).OrderBy(u => u.Fullname).ToList();

                var shopIds = users.Where(u => u.ShopId.HasValue).Select(u => u.ShopId!.Value).Distinct().ToList();
                var shopNames = await GetShopNamesAsync(shopIds);

                var result = users.Select(user => new UserSummaryDto
                {
                    Id = user.Id,
                    Username = user.UserName!,
                    Fullname = user.Fullname,
                    Role = user.Roles.FirstOrDefault() ?? "attendant",
                    ShopName = user.ShopId.HasValue ? shopNames.GetValueOrDefault(user.ShopId.Value) : null
                }).ToList();

                return result;
            }
            catch (Exception ex)
            {
                return Error.Failure(description: ex.Message);
            }
        }

        public async Task<ErrorOr<SuccessResponse>> AdminResetPasswordAsync(string userId, string newPassword)
        {
            try
            {
                var user = await userManager.FindByIdAsync(userId);
                if (user is null) return Error.NotFound(description: "User not found");

                var resetToken = await userManager.GeneratePasswordResetTokenAsync(user);
                var result = await userManager.ResetPasswordAsync(user, resetToken, newPassword);
                if (!result.Succeeded) return Error.Failure(description: "Could not set the new password");

                return new SuccessResponse { Message = "Password reset successfully" };
            }
            catch (Exception ex)
            {
                return Error.Failure(description: ex.Message);
            }
        }

        public async Task<ErrorOr<SuccessResponse>> RemoveUserAsync(string userId)
        {
            try
            {
                var user = await userManager.FindByIdAsync(userId);
                if (user is null) return Error.NotFound(description: "User not found");

                var roles = await userManager.GetRolesAsync(user);
                if (roles.Contains("admin"))
                {
                    var admins = await userManager.GetUsersInRoleAsync("admin");
                    if (admins.Count <= 1) return Error.Validation(description: "At least one admin account must remain");
                }

                var result = await userManager.DeleteAsync(user);
                if (!result.Succeeded) return Error.Failure(description: "Could not remove the user");

                return new SuccessResponse { Message = "User removed successfully" };
            }
            catch (Exception ex)
            {
                return Error.Failure(description: ex.Message);
            }
        }

        public async Task<ErrorOr<SuccessResponse>> RequestPasswordResetAsync(string username)
        {
            try
            {
                var user = await userManager.FindByNameAsync(username);
                if (user == null) return Error.NotFound(description: "No account found with that User ID");

                var id = Guid.NewGuid();
                await PasswordResetRequests.Document(id.ToString()).SetAsync(new Dictionary<string, object?>
                {
                    ["UserId"] = user.Id,
                    ["IsApproved"] = null,
                    ["RequestedAt"] = DateTime.UtcNow,
                    ["UpdatedAt"] = DateTime.UtcNow,
                });

                return new SuccessResponse { Message = "Your admin has been notified" };
            }
            catch (Exception ex)
            {
                return Error.Failure(description: ex.Message);
            }
        }

        public async Task<ErrorOr<List<PendingPasswordResetDto>>> GetPasswordResetRequestsAsync()
        {
            try
            {
                var snapshot = await PasswordResetRequests.WhereEqualTo("IsApproved", null).GetSnapshotAsync();
                var requests = snapshot.Documents.ToList();

                var result = new List<PendingPasswordResetDto>();
                foreach (var request in requests)
                {
                    var userId = request.GetValue<string>("UserId");
                    var userSnapshot = await Users.Document(userId).GetSnapshotAsync();

                    result.Add(new PendingPasswordResetDto
                    {
                        Id = Guid.Parse(request.Id),
                        Username = userSnapshot.Exists ? userSnapshot.GetValue<string?>("UserName") ?? "" : "",
                        FullName = userSnapshot.Exists ? userSnapshot.GetValue<string?>("Fullname") ?? "" : "",
                        RequestedAt = request.GetValue<DateTime>("RequestedAt")
                    });
                }

                return result.OrderBy(r => r.RequestedAt).ToList();
            }
            catch (Exception ex)
            {
                return Error.Failure(description: ex.Message);
            }
        }

        public async Task<ErrorOr<SuccessResponse>> ResolvePasswordResetRequestAsync(Guid id, string newPassword)
        {
            try
            {
                var requestRef = PasswordResetRequests.Document(id.ToString());
                var request = await requestRef.GetSnapshotAsync();
                if (!request.Exists) return Error.NotFound(description: "Request not found");

                var userId = request.GetValue<string>("UserId");
                var user = await userManager.FindByIdAsync(userId);
                if (user is null) return Error.NotFound(description: "User not found");

                var resetToken = await userManager.GeneratePasswordResetTokenAsync(user);
                var result = await userManager.ResetPasswordAsync(user, resetToken, newPassword);
                if (!result.Succeeded) return Error.Failure(description: "Could not set the new password");

                await requestRef.UpdateAsync(new Dictionary<string, object>
                {
                    ["IsApproved"] = true,
                    ["UpdatedAt"] = DateTime.UtcNow,
                });

                return new SuccessResponse { Message = "Password reset resolved" };
            }
            catch (Exception ex)
            {
                return Error.Failure(description: ex.Message);
            }
        }

        private async Task<string?> GetShopNameAsync(Guid? shopId)
        {
            if (!shopId.HasValue) return null;

            var shopSnapshot = await Shops.Document(shopId.Value.ToString()).GetSnapshotAsync();
            return shopSnapshot.Exists ? shopSnapshot.GetValue<string?>("DisplayName") : null;
        }

        private async Task<Dictionary<Guid, string>> GetShopNamesAsync(List<Guid> shopIds)
        {
            var result = new Dictionary<Guid, string>();
            if (shopIds.Count == 0) return result;

            foreach (var chunk in shopIds.Chunk(30))
            {
                var refs = chunk.Select(id => Shops.Document(id.ToString())).ToList();
                var snapshots = await db.GetAllSnapshotsAsync(refs);
                foreach (var snapshot in snapshots)
                {
                    if (snapshot.Exists) result[Guid.Parse(snapshot.Id)] = snapshot.GetValue<string?>("DisplayName") ?? "";
                }
            }

            return result;
        }
    }

    public class Token
    {
        [JsonProperty("accessToken")]
        public string AccessToken { get; set; } = null!;

        [JsonProperty("expiresIn")]
        public int ExpiresIn { get; set; }
    }

    public class MeResponse
    {
        [JsonProperty("userId")]
        public string UserId { get; set; } = null!;

        [JsonProperty("username")]
        public string Username { get; set; } = null!;

        [JsonProperty("fullname")]
        public string Fullname { get; set; } = null!;

        [JsonProperty("shopId")]
        public Guid? ShopId { get; set; }

        [JsonProperty("shopName")]
        public string? ShopName { get; set; }

        [JsonProperty("role")]
        public string Role { get; set; } = null!;
    }

    public class UserSummaryDto
    {
        [JsonProperty("id")]
        public string Id { get; set; } = null!;

        [JsonProperty("username")]
        public string Username { get; set; } = null!;

        [JsonProperty("fullname")]
        public string Fullname { get; set; } = null!;

        [JsonProperty("role")]
        public string Role { get; set; } = null!;

        [JsonProperty("shopName")]
        public string? ShopName { get; set; }
    }

    public class PendingPasswordResetDto
    {
        [JsonProperty("id")]
        public Guid Id { get; set; }

        [JsonProperty("username")]
        public string Username { get; set; } = null!;

        [JsonProperty("fullName")]
        public string FullName { get; set; } = null!;

        [JsonProperty("requestedAt")]
        public DateTime RequestedAt { get; set; }
    }
}
