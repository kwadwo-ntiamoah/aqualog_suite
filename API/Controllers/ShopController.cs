
using API.Infrastructure.Services;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace API.Controllers
{
    [Route("api/[controller]")]
    public class ShopController(ShopService shopService) : ParentController
    {
        [HttpGet]
        public async Task<IActionResult> GetShopDetailsAsync()
        {
            return Ok();
        }

        [HttpGet("list")]
        public async Task<IActionResult> ListShopsAsync()
        {
            var response = await shopService.GetShopsAsync();
            return response.Match(Ok, Problem);
        }

        [HttpPost]
        public async Task<IActionResult> AddShopAsync(AddShopDto request)
        {
            var response = await shopService.AddShopAsync(request);
            return response.Match(Ok, Problem);
        }

        [HttpPost("{id}/balance")]
        public async Task<IActionResult> RecordShopBalance(Guid id, AddShopBalanceDto request)
        {
            var userId = GetUserId();

            var response = await shopService.RecordShopPowerBalanceAsync(id, userId, request);
            return response.Match(Ok, Problem);
        }

        [HttpGet("{id}/balance")]
        public async Task<IActionResult> GetLatestBalanceAsync(Guid id)
        {
            var response = await shopService.GetLatestBalanceAsync(id);
            return response.Match(Ok, Problem);
        }

        [HttpGet("{id}/summary")]
        public async Task<IActionResult> GetShopSummaryAsync(Guid id)
        {
            var response = await shopService.GetShopSummaryAsync(id);
            return response.Match(Ok, Problem);
        }
    }

    public class AddShopDto
    {
        [JsonProperty("name")]
        public string Name { get; set; } = null!;
    }

    public class AddShopBalanceDto
    {
        public int Balance { get; set; }
    }
}