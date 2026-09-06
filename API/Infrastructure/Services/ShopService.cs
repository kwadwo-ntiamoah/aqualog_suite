using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using API.Controllers;
using API.Infrastructure.Persistence;
using API.Models;
using ErrorOr;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using static API.Infrastructure.Services.Common;

namespace API.Infrastructure.Services
{
    public class ShopService(AppDbContext context)
    {
        public async Task<ErrorOr<List<ShopSummaryDto>>> GetShopsAsync()
        {
            try
            {
                var shops = await context.Shops
                    .Where(s => s.IsActive)
                    .OrderBy(s => s.DisplayName)
                    .Select(s => new ShopSummaryDto { Id = s.Id, Name = s.DisplayName })
                    .ToListAsync();

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
                await context.Shops.AddAsync(new Shop
                {
                    CreatedAt = DateTime.UtcNow,
                    DisplayName = model.Name,
                    IsActive = true
                });

                var rowsAffected = await context.SaveChangesAsync();
                if (rowsAffected > 0) return new SuccessResponse { Message = "Driver added successfully" };

                return Error.Failure(description: "Error adding driver");
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
                var shop = await context.Shops.FindAsync(shopId);
                if (shop == null) return Error.NotFound(description: "Shop not found");

                await context.ElectricityBalances.AddAsync(new ElectricityBalance
                {
                    ShopId = shopId,
                    Balance = model.Balance,
                    DateRecorded = DateTime.UtcNow,
                    UserId = userId
                });

                var rowsAffected = await context.SaveChangesAsync();
                if (rowsAffected > 0) return new SuccessResponse { Message = "Balance added successfully" };

                return Error.Failure(description: "Error adding driver");
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
                var latest = await context.ElectricityBalances
                    .Where(b => b.ShopId == shopId)
                    .OrderByDescending(b => b.DateRecorded)
                    .Select(b => new ElectricityBalanceDto
                    {
                        HasRecord = true,
                        Balance = b.Balance,
                        DateRecorded = b.DateRecorded,
                        RecordedByName = b.User != null ? b.User.Fullname : null
                    })
                    .FirstOrDefaultAsync();

                return latest ?? new ElectricityBalanceDto { HasRecord = false };
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

                var todaysTransactions = await context.Transactions
                    .Where(t => t.ShopId == shopId && t.DateCreated >= today && t.DateCreated < tomorrow)
                    .ToListAsync();

                return new ShopSummaryStatsDto
                {
                    SalesToday = todaysTransactions.Count,
                    CollectedToday = todaysTransactions.Sum(t => t.TotalAmount)
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