using System.Linq;
using API.Infrastructure.Persistence;
using API.Models;
using ErrorOr;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using static API.Infrastructure.Services.Common;

namespace API.Infrastructure.Services
{
    public class AuthService(UserManager<AppUser> userManager, TokenService tokenService, AppDbContext context)
    {
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
                var user = await userManager.Users
                    .Include(u => u.Shop)
                    .FirstOrDefaultAsync(u => u.Id == userId);

                if (user is null) return Error.Unauthorized(description: "Not authorized to perform this action");

                var roles = await userManager.GetRolesAsync(user);

                return new MeResponse
                {
                    UserId = user.Id,
                    Username = user.UserName!,
                    Fullname = user.Fullname,
                    ShopId = user.ShopId,
                    ShopName = user.Shop?.DisplayName,
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
                var users = await userManager.Users
                    .Include(u => u.Shop)
                    .OrderBy(u => u.Fullname)
                    .ToListAsync();

                var result = new List<UserSummaryDto>();
                foreach (var user in users)
                {
                    var roles = await userManager.GetRolesAsync(user);
                    result.Add(new UserSummaryDto
                    {
                        Id = user.Id,
                        Username = user.UserName!,
                        Fullname = user.Fullname,
                        Role = roles.FirstOrDefault() ?? "attendant",
                        ShopName = user.Shop?.DisplayName
                    });
                }

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

                await context.PasswordResetRequests.AddAsync(new PasswordResetRequest
                {
                    UserId = user.Id,
                    IsApproved = null,
                    RequestedAt = DateTime.UtcNow
                });

                await context.SaveChangesAsync();
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
                var requests = await context.PasswordResetRequests
                    .Where(r => r.IsApproved == null)
                    .OrderBy(r => r.RequestedAt)
                    .Select(r => new PendingPasswordResetDto
                    {
                        Id = r.Id,
                        Username = r.User != null ? r.User.UserName! : "",
                        FullName = r.User != null ? r.User.Fullname : "",
                        RequestedAt = r.RequestedAt
                    })
                    .ToListAsync();

                return requests;
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
                var request = await context.PasswordResetRequests.FindAsync(id);
                if (request is null) return Error.NotFound(description: "Request not found");

                var user = await userManager.FindByIdAsync(request.UserId);
                if (user is null) return Error.NotFound(description: "User not found");

                var resetToken = await userManager.GeneratePasswordResetTokenAsync(user);
                var result = await userManager.ResetPasswordAsync(user, resetToken, newPassword);
                if (!result.Succeeded) return Error.Failure(description: "Could not set the new password");

                request.IsApproved = true;
                request.UpdatedAt = DateTime.UtcNow;
                await context.SaveChangesAsync();

                return new SuccessResponse { Message = "Password reset resolved" };
            }
            catch (Exception ex)
            {
                return Error.Failure(description: ex.Message);
            }
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