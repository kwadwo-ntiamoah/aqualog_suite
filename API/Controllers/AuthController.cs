using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using API.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace API.Controllers
{
    [Route("api/[controller]")]
    public class AuthController(AuthService authService) : ParentController
    {
        [AllowAnonymous]
        [HttpPost("token")]
        public async Task<IActionResult> GetTokenAsync(AuthDto request)
        {
            var response = await authService.GetTokenAsync(request.Username, request.Password);
            return response.Match(Ok, Problem);
        }

        [HttpGet("me")]
        public async Task<IActionResult> GetMeAsync()
        {
            var userId = GetUserId();
            var response = await authService.GetMeAsync(userId);
            return response.Match(Ok, Problem);
        }

        [HttpPost("user")]
        public async Task<IActionResult> AddUserAsync(AddUserDto request)
        {
            var response = await authService.AddUserAsync(request.Username, request.Fullname, request.TempPassword, request.ShopId);
            return response.Match(Ok, Problem);
        }

        [Authorize(Roles = "admin")]
        [HttpGet("users")]
        public async Task<IActionResult> GetUsersAsync()
        {
            var response = await authService.GetUsersAsync();
            return response.Match(Ok, Problem);
        }

        [Authorize(Roles = "admin")]
        [HttpPost("users/{id}/reset")]
        public async Task<IActionResult> AdminResetPasswordAsync(string id, ResolvePasswordResetDto request)
        {
            var response = await authService.AdminResetPasswordAsync(id, request.NewPassword);
            return response.Match(Ok, Problem);
        }

        [Authorize(Roles = "admin")]
        [HttpPost("users/{id}/remove")]
        public async Task<IActionResult> RemoveUserAsync(string id)
        {
            var response = await authService.RemoveUserAsync(id);
            return response.Match(Ok, Problem);
        }

        [HttpPost("password/reset")]
        public async Task<IActionResult> ResetPasswordAsync()
        {
            var userId = GetUserId();
            var response = await authService.ResetPasswordAsync(userId);

            return response.Match(Ok, Problem);
        }

        [HttpPost("password/change")]
        public async Task<IActionResult> ChangePasswordAsync(ChangePasswordDto request)
        {
            var userId = GetUserId();
            var response = await authService.ChangePasswordAsync(userId, request.OldPassword, request.NewPassword);

            return response.Match(Ok, Problem);
        }

        [AllowAnonymous]
        [HttpPost("password/reset-request")]
        public async Task<IActionResult> RequestPasswordResetAsync(RequestPasswordResetDto request)
        {
            var response = await authService.RequestPasswordResetAsync(request.Username);
            return response.Match(Ok, Problem);
        }

        [Authorize(Roles = "admin")]
        [HttpGet("password/reset-requests")]
        public async Task<IActionResult> GetPasswordResetRequestsAsync()
        {
            var response = await authService.GetPasswordResetRequestsAsync();
            return response.Match(Ok, Problem);
        }

        [Authorize(Roles = "admin")]
        [HttpPost("password/reset-requests/{id}/resolve")]
        public async Task<IActionResult> ResolvePasswordResetRequestAsync(Guid id, ResolvePasswordResetDto request)
        {
            var response = await authService.ResolvePasswordResetRequestAsync(id, request.NewPassword);
            return response.Match(Ok, Problem);
        }
    }


    public class AuthDto
    {
        [JsonProperty("username")]
        public string Username { get; set; } = null!;

        [JsonProperty("password")]
        public string Password { get; set; } = null!;
    }

    public class AddUserDto
    {
        [JsonProperty("shop")]
        public Guid ShopId {get; set;}

        [JsonProperty("username")]
        public string Username { get; set; } = null!;

        [JsonProperty("fullname")]
        public string Fullname { get; set; } = null!;

        [JsonProperty("tempPassword")]
        public string TempPassword { get; set; } = null!;
    }

    public class ChangePasswordDto
    {
        [JsonProperty("oldPassword")]
        public string OldPassword { get; set; } = null!;

        [JsonProperty("newPassword")]
        public string NewPassword { get; set; } = null!;
    }

    public class RequestPasswordResetDto
    {
        [JsonProperty("username")]
        public string Username { get; set; } = null!;
    }

    public class ResolvePasswordResetDto
    {
        [JsonProperty("newPassword")]
        public string NewPassword { get; set; } = null!;
    }
}