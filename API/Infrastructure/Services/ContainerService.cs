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
    public class ContainerService(AppDbContext context)
    {
        public async Task<ErrorOr<List<ContainerSummaryDto>>> GetContainersAsync()
        {
            try
            {
                var containers = await context.Containers
                    .Where(c => c.IsActive)
                    .OrderByDescending(c => c.CreatedDate)
                    .Select(c => new ContainerSummaryDto
                    {
                        Id = c.Id,
                        Type = c.Type.ToString(),
                        DisplayName = c.DisplayName,
                        UnitPrice = c.UnitPrice
                    })
                    .ToListAsync();

                return containers;
            }
            catch (Exception ex)
            {
                return Error.Failure(description: ex.Message);
            }
        }

        public async Task<ErrorOr<SuccessResponse>> AddContainerAsync(AddContainerDto model)
        {
            try
            {
                await context.Containers.AddAsync(new Container
                {
                    CreatedDate = DateTime.UtcNow,
                    DisplayName = model.DisplayName,
                    Type = model.Type,
                    UnitPrice = model.UnitPrice
                });

                var rowsAffected = await context.SaveChangesAsync();
                if (rowsAffected > 0) return new SuccessResponse { Message = "Container added successfully" };

                return Error.Failure(description: "Error adding container");
            }
            catch (Exception ex)
            {
                return Error.Failure(description: ex.Message);
            }
        }

        public async Task<ErrorOr<SuccessResponse>> ToggleContainerStatusAsync(Guid containerId)
        {
            try
            {
                var container = await context.Containers.FindAsync(containerId);
                if (container is null) return Error.NotFound(description: "Container not found");

                container.IsActive = !container.IsActive;

                var rowsAffected = await context.SaveChangesAsync();
                if (rowsAffected > 0) return new SuccessResponse { Message = "Container updated successfully" };

                return Error.Failure(description: "Error adding container");
            }
            catch (Exception ex)
            {
                return Error.Failure(description: ex.Message);
            }
        }

        public async Task<ErrorOr<SuccessResponse>> UpdateContainerAsync(Guid containerId, UpdateContainerDto model)
        {
            try
            {
                var container = await context.Containers.FindAsync(containerId);
                if (container is null) return Error.NotFound(description: "Container not found");

                container.UnitPrice = model.UnitPrice;

                // Setting the price to the value it already has is a legitimate
                // no-op — SaveChangesAsync correctly reports 0 rows affected in
                // that case, so success can't hinge on rowsAffected here like the
                // add/create methods above do.
                await context.SaveChangesAsync();
                return new SuccessResponse { Message = "Container updated successfully" };
            }
            catch (Exception ex)
            {
                return Error.Failure(description: ex.Message);
            }
        }
    }

    public class ContainerSummaryDto
    {
        [JsonProperty("id")]
        public Guid Id { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; } = null!;

        [JsonProperty("displayName")]
        public string DisplayName { get; set; } = null!;

        [JsonProperty("unitPrice")]
        public decimal UnitPrice { get; set; }
    }
}