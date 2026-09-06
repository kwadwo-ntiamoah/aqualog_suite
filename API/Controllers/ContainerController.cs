using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using API.Infrastructure.Services;
using API.Models;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace API.Controllers
{
    [Route("api/[controller]")]
    public class ContainerController(ContainerService containerService) : ParentController
    {
        [HttpGet]
        public async Task<IActionResult> GetContainersAsync()
        {
            var response = await containerService.GetContainersAsync();
            return response.Match(Ok, Problem);
        }

        [HttpPost]
        public async Task<IActionResult> AddContainerAsync(AddContainerDto request)
        {
            var response = await containerService.AddContainerAsync(request);
            return response.Match(Ok, Problem);
        }

        [HttpPost("{id}/update")]
        public async Task<IActionResult> UpdateContainerAsync(Guid id, [FromBody] UpdateContainerDto request)
        {
            var response = await containerService.UpdateContainerAsync(id, request);
            return response.Match(Ok, Problem);
        }

        [HttpPost("{id}/toggleStatus")]
        public async Task<IActionResult> ToggleStatusAsync(Guid id)
        {
            var response = await containerService.ToggleContainerStatusAsync(id);
            return response.Match(Ok, Problem);
        }
    }

    public class AddContainerDto
    {
        [JsonProperty("unitPrice")]
        public decimal UnitPrice { get; set; }

        [JsonProperty("type")]
        public ContainerType Type { get; set; } = ContainerType.TANK;

        [JsonProperty("displayName")]
        public string DisplayName { get; set; } = null!;
    }

    public class UpdateContainerDto
    {
        [JsonProperty("unitPrice")]
        public decimal UnitPrice { get; set; }
    }
}