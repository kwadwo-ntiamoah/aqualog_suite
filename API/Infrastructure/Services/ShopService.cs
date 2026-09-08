using API.Controllers;
using API.Models;
using ErrorOr;
using Google.Cloud.Firestore;
using Newtonsoft.Json;
using static API.Infrastructure.Services.Common;

namespace API.Infrastructure.Services
{
    public class ShopService(FirestoreDb db)
    {
        private CollectionReference Shops => db.Collection("shops");
        private CollectionReference Balances => db.Collection("electricityBalances");
        private CollectionReference Transactions => db.Collection("transactions");
        private CollectionReference Users => db.Collection("users");

        public async Task<ErrorOr<List<ShopSummaryDto>>> GetShopsAsync()
        {
            try
            {
                var snapshot = await Shops.WhereEqualTo("IsActive", true).GetSnapshotAsync();

                var shops = snapshot.Documents
                    .Select(d => new ShopSummaryDto { Id = Guid.Parse(d.Id), Name = d.GetValue<string>("DisplayName") })
                    .OrderBy(s => s.Name)
                    .ToList();

                return shops;
            }
            catch (Exception ex)
            {
                return Error.Failure(description: ex.Message);
            }
        }

        public async Task<ErrorOr<SuccessResponse>> AddShopAsync(AddShopDto model)
        {
            try
            {
                var id = Guid.NewGuid();
                await Shops.Document(id.ToString()).SetAsync(new Dictionary<string, object>
                {
                    ["DisplayName"] = model.Name,
                    ["IsActive"] = true,
                    ["CreatedAt"] = DateTime.UtcNow,
                    ["UpdatedAt"] = DateTime.UtcNow,
                });

                return new SuccessResponse { Message = "Driver added successfully" };
            }
            catch (Exception ex)
            {
                return Error.Failure(description: ex.Message);
            }
        }

        public async Task<ErrorOr<SuccessResponse>> RecordShopPowerBalanceAsync(Guid shopId, string userId, AddShopBalanceDto model)
        {
            try
            {
                var shopSnapshot = await Shops.Document(shopId.ToString()).GetSnapshotAsync();
                if (!shopSnapshot.Exists) return Error.NotFound(description: "Shop not found");

                var id = Guid.NewGuid();
                await Balances.Document(id.ToString()).SetAsync(new Dictionary<string, object>
                {
                    ["ShopId"] = shopId.ToString(),
                    ["Balance"] = model.Balance,
                    ["DateRecorded"] = DateTime.UtcNow,
                    ["UserId"] = userId,
                });

                return new SuccessResponse { Message = "Balance added successfully" };
            }
            catch (Exception ex)
            {
                return Error.Failure(description: ex.Message);
            }
        }

        public async Task<ErrorOr<ElectricityBalanceDto>> GetLatestBalanceAsync(Guid shopId)
        {
            try
            {
                var snapshot = await Balances
                    .WhereEqualTo("ShopId", shopId.ToString())
                    .OrderByDescending("DateRecorded")
                    .Limit(1)
                    .GetSnapshotAsync();

                var latest = snapshot.Documents.FirstOrDefault();
                if (latest is null) return new ElectricityBalanceDto { HasRecord = false };

                var userId = latest.GetValue<string>("UserId");
                var userSnapshot = await Users.Document(userId).GetSnapshotAsync();
                var recordedByName = userSnapshot.Exists ? userSnapshot.GetValue<string?>("Fullname") : null;

                return new ElectricityBalanceDto
                {
                    HasRecord = true,
                    Balance = latest.GetValue<int>("Balance"),
                    DateRecorded = latest.GetValue<DateTime>("DateRecorded"),
                    RecordedByName = recordedByName
                };
            }
            catch (Exception ex)
            {
                return Error.Failure(description: ex.Message);
            }
        }

        public async Task<ErrorOr<ShopSummaryStatsDto>> GetShopSummaryAsync(Guid shopId)
        {
            try
            {
                var today = DateTime.UtcNow.Date;
                var tomorrow = today.AddDays(1);

                var snapshot = await Transactions
                    .WhereEqualTo("ShopId", shopId.ToString())
                    .WhereGreaterThanOrEqualTo("DateCreated", today)
                    .WhereLessThan("DateCreated", tomorrow)
                    .GetSnapshotAsync();

                var totalAmount = snapshot.Documents.Sum(d => decimal.Parse(d.GetValue<string>("TotalAmount")));

                return new ShopSummaryStatsDto
                {
                    SalesToday = snapshot.Count,
                    CollectedToday = totalAmount
                };
            }
            catch (Exception ex)
            {
                return Error.Failure(description: ex.Message);
            }
        }
    }

    public class ElectricityBalanceDto
    {
        [JsonProperty("hasRecord")]
        public bool HasRecord { get; set; }

        [JsonProperty("balance")]
        public int Balance { get; set; }

        [JsonProperty("dateRecorded")]
        public DateTime? DateRecorded { get; set; }

        [JsonProperty("recordedByName")]
        public string? RecordedByName { get; set; }
    }

    public class ShopSummaryStatsDto
    {
        [JsonProperty("salesToday")]
        public int SalesToday { get; set; }

        [JsonProperty("collectedToday")]
        public decimal CollectedToday { get; set; }
    }

    public class ShopSummaryDto
    {
        [JsonProperty("id")]
        public Guid Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; } = null!;
    }
}
