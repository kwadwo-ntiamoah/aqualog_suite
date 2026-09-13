using ErrorOr;
using Google.Cloud.Firestore;
using Newtonsoft.Json;

namespace API.Infrastructure.Services
{
    public class AnalyticsService(FirestoreDb db)
    {
        private CollectionReference Transactions => db.Collection("transactions");
        private CollectionReference Shops => db.Collection("shops");
        private CollectionReference Balances => db.Collection("electricityBalances");
        private CollectionReference Users => db.Collection("users");

        public async Task<ErrorOr<AnalyticsOverviewDto>> GetOverviewAsync()
        {
            try
            {
                var today = DateTime.UtcNow.Date;
                var sevenDaysAgo = today.AddDays(-6);

                var snapshot = await Transactions.WhereGreaterThanOrEqualTo("DateCreated", sevenDaysAgo).GetSnapshotAsync();

                var recentTransactions = snapshot.Documents
                    .Select(d => new
                    {
                        DateCreated = d.GetValue<DateTime>("DateCreated"),
                        TotalAmount = decimal.Parse(d.GetValue<string>("TotalAmount")),
                        PaymentMethod = d.GetValue<string>("PaymentMethod"),
                        Container = d.GetValue<string>("Container"),
                        VehicleNo = d.GetValue<string?>("VehicleNo"),
                        Quantity = d.GetValue<int>("Quantity"),
                    })
                    .ToList();

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

                var topVehicles = recentTransactions
                    .Where(t => t.Container == "TANK" && t.VehicleNo != null)
                    .GroupBy(t => t.VehicleNo)
                    .Select(g => new TopVehicleDto { VehicleNo = g.Key!, TanksSold = g.Sum(t => t.Quantity) })
                    .OrderByDescending(v => v.TanksSold)
                    .Take(5)
                    .ToList();

                var electricityBalances = await GetElectricityStatusByShopAsync();

                return new AnalyticsOverviewDto
                {
                    RevenueLast7Days = revenueByDay,
                    CashPercentage = cashPct,
                    MomoPercentage = momoPct,
                    TopVehicles = topVehicles,
                    ElectricityBalances = electricityBalances
                };
            }
            catch (Exception ex)
            {
                return Error.Failure(description: ex.Message);
            }
        }

        // Current electricity balance per shop, each with who last recorded it
        // and when — one query per shop (not batched, since Firestore has no
        // native "latest per group" query), fine at this app's shop count.
        private async Task<List<ShopElectricityStatusDto>> GetElectricityStatusByShopAsync()
        {
            var shopsSnapshot = await Shops.WhereEqualTo("IsActive", true).GetSnapshotAsync();
            var result = new List<ShopElectricityStatusDto>();

            foreach (var shop in shopsSnapshot.Documents)
            {
                var shopId = shop.Id;
                var shopName = shop.GetValue<string>("DisplayName");

                var balanceSnapshot = await Balances
                    .WhereEqualTo("ShopId", shopId)
                    .OrderByDescending("DateRecorded")
                    .Limit(1)
                    .GetSnapshotAsync();

                var latest = balanceSnapshot.Documents.FirstOrDefault();
                if (latest is null)
                {
                    result.Add(new ShopElectricityStatusDto { ShopId = Guid.Parse(shopId), ShopName = shopName, HasRecord = false });
                    continue;
                }

                var userId = latest.GetValue<string>("UserId");
                var userSnapshot = await Users.Document(userId).GetSnapshotAsync();
                var recordedByName = userSnapshot.Exists ? userSnapshot.GetValue<string?>("Fullname") : null;

                result.Add(new ShopElectricityStatusDto
                {
                    ShopId = Guid.Parse(shopId),
                    ShopName = shopName,
                    HasRecord = true,
                    Balance = latest.GetValue<int>("Balance"),
                    DateRecorded = latest.GetValue<DateTime>("DateRecorded"),
                    RecordedByName = recordedByName
                });
            }

            return result.OrderBy(s => s.ShopName).ToList();
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

        [JsonProperty("electricityBalances")]
        public List<ShopElectricityStatusDto> ElectricityBalances { get; set; } = [];
    }

    public class ShopElectricityStatusDto
    {
        [JsonProperty("shopId")]
        public Guid ShopId { get; set; }

        [JsonProperty("shopName")]
        public string ShopName { get; set; } = null!;

        [JsonProperty("hasRecord")]
        public bool HasRecord { get; set; }

        [JsonProperty("balance")]
        public int Balance { get; set; }

        [JsonProperty("dateRecorded")]
        public DateTime? DateRecorded { get; set; }

        [JsonProperty("recordedByName")]
        public string? RecordedByName { get; set; }
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
