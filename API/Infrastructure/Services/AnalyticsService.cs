using API.Infrastructure.Persistence;
using API.Models;
using ErrorOr;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace API.Infrastructure.Services
{
    public class AnalyticsService(AppDbContext context)
    {
        public async Task<ErrorOr<AnalyticsOverviewDto>> GetOverviewAsync()
        {
            try
            {
                var today = DateTime.UtcNow.Date;
                var sevenDaysAgo = today.AddDays(-6);

                var recentTransactions = await context.Transactions
                    .Where(t => t.DateCreated >= sevenDaysAgo)
                    .ToListAsync();

                var revenueByDay = new List<DailyRevenueDto>();
                for (var day = sevenDaysAgo; day <= today; day = day.AddDays(1))
                {
                    var dayTotal = recentTransactions.Where(t => t.DateCreated.Date == day).Sum(t => t.TotalAmount);
                    revenueByDay.Add(new DailyRevenueDto { Date = day, Amount = dayTotal });
                }

                var totalRevenue = recentTransactions.Sum(t => t.TotalAmount);
                var cashRevenue = recentTransactions.Where(t => t.PaymentMethod == "cash").Sum(t => t.TotalAmount);
                var momoRevenue = recentTransactions.Where(t => t.PaymentMethod == "momo").Sum(t => t.TotalAmount);

                var cashPct = totalRevenue > 0 ? Math.Round(cashRevenue / totalRevenue * 100, 1) : 0;
                var momoPct = totalRevenue > 0 ? Math.Round(momoRevenue / totalRevenue * 100, 1) : 0;

                var topVehicles = await context.Transactions
                    .Where(t => t.Container == ContainerType.TANK && t.VehicleNo != null)
                    .GroupBy(t => t.VehicleNo)
                    .Select(g => new TopVehicleDto { VehicleNo = g.Key!, TanksSold = g.Sum(t => t.Quantity) })
                    .OrderByDescending(v => v.TanksSold)
                    .Take(5)
                    .ToListAsync();

                return new AnalyticsOverviewDto
                {
                    RevenueLast7Days = revenueByDay,
                    CashPercentage = cashPct,
                    MomoPercentage = momoPct,
                    TopVehicles = topVehicles
                };
            }
            catch (Exception ex)
            {
                return Error.Failure(description: ex.Message);
            }
        }
    }

    public class AnalyticsOverviewDto
    {
        [JsonProperty("revenueLast7Days")]
        public List<DailyRevenueDto> RevenueLast7Days { get; set; } = [];

        [JsonProperty("cashPercentage")]
        public decimal CashPercentage { get; set; }

        [JsonProperty("momoPercentage")]
        public decimal MomoPercentage { get; set; }

        [JsonProperty("topVehicles")]
        public List<TopVehicleDto> TopVehicles { get; set; } = [];
    }

    public class DailyRevenueDto
    {
        [JsonProperty("date")]
        public DateTime Date { get; set; }

        [JsonProperty("amount")]
        public decimal Amount { get; set; }
    }

    public class TopVehicleDto
    {
        [JsonProperty("vehicleNo")]
        public string VehicleNo { get; set; } = null!;

        [JsonProperty("tanksSold")]
        public int TanksSold { get; set; }
    }
}
