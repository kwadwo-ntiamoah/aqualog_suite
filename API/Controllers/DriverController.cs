using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using API.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DriverController(DriverService driverService) : ParentController
    {
        [Authorize(Roles = "admin")]
        [HttpGet]
        public async Task<IActionResult> GetDriversAsync()
        {
            var response = await driverService.GetDriversAsync();
            return response.Match(Ok, Problem);
        }

        [HttpGet("{regNo}")]
        public async Task<IActionResult> SearchDriverAsync(string regNo)
        {
            var response = await driverService.GetDriverAsync(regNo);
            return response.Match(Ok, Problem);
        }

        [Authorize(Roles = "admin")]
        [HttpPost("{id}/capacity")]
        public async Task<IActionResult> UpdateCapacityAsync(Guid id, UpdateCapacityDto request)
        {
            var response = await driverService.UpdateCapacityAsync(id, request.TanksInTruck);
            return response.Match(Ok, Problem);
        }

        [Authorize(Roles = "admin")]
        [HttpPost("bulk-upload")]
        public async Task<IActionResult> BulkUploadAsync(IFormFile file)
        {
            var response = await driverService.BulkUploadAsync(file);
            return response.Match(Ok, Problem);
        }

        [HttpPost]
        public async Task<IActionResult> AddDriverAsync(AddDriverDo request)
        {
            var response = await driverService.AddDriverAsync(request);
            return response.Match(Ok, Problem);
        }

        [HttpPost("request")]
        public async Task<IActionResult> RequestDriverAsync(AddDriverDo request)
        {
            var userId = GetUserId();
            var response = await driverService.RequestDriverAsync(request, userId);
            return response.Match(Ok, Problem);
        }

        [Authorize(Roles = "admin")]
        [HttpGet("requests")]
        public async Task<IActionResult> GetDriverRequestsAsync()
        {
            var response = await driverService.GetDriverRequestsAsync();
            return response.Match(Ok, Problem);
        }

        [Authorize(Roles = "admin")]
        [HttpPost("requests/{id}/approve")]
        public async Task<IActionResult> ApproveDriverRequestAsync(Guid id)
        {
            var response = await driverService.ApproveDriverRequestAsync(id);
            return response.Match(Ok, Problem);
        }

        [Authorize(Roles = "admin")]
        [HttpPost("requests/{id}/reject")]
        public async Task<IActionResult> RejectDriverRequestAsync(Guid id)
        {
            var response = await driverService.RejectDriverRequestAsync(id);
            return response.Match(Ok, Problem);
        }
    }

    public class AddDriverDo
    {
        [JsonProperty("name")]
        public string Name { get; set; } = null!;

        [JsonProperty("regNo")]
        public string RegNo { get; set; } = null!;

        [JsonProperty("contact")]
        public string Contact { get; set; } = null!;

        [JsonProperty("tanksInTruck")]
        public int TanksInTruck { get; set; }
    }

    public class UpdateCapacityDto
    {
        [JsonProperty("tanksInTruck")]
        public int TanksInTruck { get; set; }
    }
}