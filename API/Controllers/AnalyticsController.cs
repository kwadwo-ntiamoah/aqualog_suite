using API.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Authorize(Roles = "admin")]
    [Route("api/[controller]")]
    public class AnalyticsController(AnalyticsService analyticsService) : ParentController
    {
        [HttpGet("overview")]
        public async Task<IActionResult> GetOverviewAsync()
        {
            var response = await analyticsService.GetOverviewAsync();
            return response.Match(Ok, Problem);
        }
    }
}
