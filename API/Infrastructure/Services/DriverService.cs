using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using API.Controllers;
using API.Infrastructure.Persistence;
using API.Models;
using ClosedXML.Excel;
using ErrorOr;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using static API.Infrastructure.Services.Common;

namespace API.Infrastructure.Services
{
    public class DriverService(AppDbContext context)
    {
        public async Task<ErrorOr<List<DriverSummaryDto>>> GetDriversAsync()
        {
            try
            {
                var drivers = await context.Drivers
                    .Where(d => d.IsActive)
                    .OrderBy(d => d.Name)
                    .Select(d => new DriverSummaryDto
                    {
                        Id = d.Id,
                        Name = d.Name,
                        RegNo = d.VehicleNo,
                        Contact = d.Contact,
                        TanksInTruck = d.TanksInTruck
                    })
                    .ToListAsync();

                return drivers;
            }
            catch (Exception ex)
            {
                return Error.Failure(description: ex.Message);
            }
        }

        public async Task<ErrorOr<SuccessResponse>> UpdateCapacityAsync(Guid id, int tanksInTruck)
        {
            try
            {
                var driver = await context.Drivers.FindAsync(id);
                if (driver is null) return Error.NotFound(description: "Vehicle not found");

                driver.TanksInTruck = tanksInTruck;
                driver.DateUpdated = DateTime.UtcNow;

                await context.SaveChangesAsync();
                return new SuccessResponse { Message = "Capacity updated successfully" };
            }
            catch (Exception ex)
            {
                return Error.Failure(description: ex.Message);
            }
        }

        public async Task<ErrorOr<SuccessResponse>> BulkUploadAsync(IFormFile file)
        {
            try
            {
                if (file is null || file.Length == 0) return Error.Validation(description: "No file uploaded");

                var newDrivers = new List<Driver>();

                using (var stream = file.OpenReadStream())
                using (var workbook = new XLWorkbook(stream))
                {
                    var worksheet = workbook.Worksheet(1);
                    var rows = worksheet.RowsUsed().Skip(1);

                    foreach (var row in rows)
                    {
                        var name = row.Cell(1).GetString().Trim();
                        var vehicleNo = row.Cell(2).GetString().Trim();
                        var contact = row.Cell(3).GetString().Trim();
                        var tanksInTruckRaw = row.Cell(4).GetString().Trim();

                        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(vehicleNo)) continue;
                        if (!int.TryParse(tanksInTruckRaw, out var tanksInTruck)) continue;

                        newDrivers.Add(new Driver
                        {
                            Name = name,
                            VehicleNo = vehicleNo,
                            Contact = contact,
                            TanksInTruck = tanksInTruck,
                            IsActive = true,
                            DateCreated = DateTime.UtcNow
                        });
                    }
                }

                if (newDrivers.Count == 0) return Error.Validation(description: "No valid rows found in the file");

                var existing = context.Drivers.ToList();
                context.Drivers.RemoveRange(existing);
                await context.Drivers.AddRangeAsync(newDrivers);

                await context.SaveChangesAsync();
                return new SuccessResponse { Message = $"Replaced fleet list with {newDrivers.Count} vehicles" };
            }
            catch (Exception ex)
            {
                return Error.Failure(description: ex.Message);
            }
        }

        public async Task<ErrorOr<SuccessResponse>> AddDriverAsync(AddDriverDo model)
        {
            try
            {
                await context.Drivers.AddAsync(new Driver
                {
                    Contact = model.Contact,
                    DateCreated = DateTime.UtcNow,
                    IsActive = true,
                    Name = model.Name,
                    TanksInTruck = model.TanksInTruck,
                    VehicleNo = model.RegNo
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

        public async Task<ErrorOr<SearchDriverResponse>> GetDriverAsync(string regNo)
        {
            try
            {
                var driver = await context.Drivers.FirstOrDefaultAsync(x => x.VehicleNo == regNo);
                if (driver == null) return Error.NotFound(description: "Driver with this vehicle No. not found");

                return new SearchDriverResponse
                {
                    Contact = driver.Contact,
                    Name = driver.Name,
                    RegNo = driver.VehicleNo,
                    TanksInTruck = driver.TanksInTruck
                };
            }
            catch (Exception ex)
            {
                return Error.Failure(description: ex.Message);
            }
        }

        public class SearchDriverResponse
        {
            [JsonProperty("name")]
            public string Name { get; set; } = null!;

            [JsonProperty("contact")]
            public string Contact { get; set; } = null!;

            [JsonProperty("tanksInTruck")]
            public int TanksInTruck { get; set; }

            [JsonProperty("regNo")]
            public string RegNo { get; set; } = null!;
        }

        public async Task<ErrorOr<SuccessResponse>> RequestDriverAsync(AddDriverDo model, string requestedById)
        {
            try
            {
                await context.AddDriverRequests.AddAsync(new AddDriverRequest
                {
                    Name = model.Name,
                    VehicleNo = model.RegNo,
                    Contact = model.Contact,
                    TanksInTruck = model.TanksInTruck,
                    IsActive = true,
                    DateRequested = DateTime.UtcNow,
                    RequestedById = requestedById
                });

                await context.SaveChangesAsync();
                return new SuccessResponse { Message = "Request submitted successfully" };
            }
            catch (Exception ex)
            {
                return Error.Failure(description: ex.Message);
            }
        }

        public async Task<ErrorOr<List<DriverRequestDto>>> GetDriverRequestsAsync()
        {
            try
            {
                var requests = await context.AddDriverRequests
                    .OrderBy(r => r.DateRequested)
                    .Select(r => new DriverRequestDto
                    {
                        Id = r.Id,
                        Name = r.Name,
                        RegNo = r.VehicleNo,
                        Contact = r.Contact,
                        TanksInTruck = r.TanksInTruck,
                        RequestedByName = r.RequestedBy != null ? r.RequestedBy.Fullname : null,
                        DateRequested = r.DateRequested
                    })
                    .ToListAsync();

                return requests;
            }
            catch (Exception ex)
            {
                return Error.Failure(description: ex.Message);
            }
        }

        public async Task<ErrorOr<SuccessResponse>> ApproveDriverRequestAsync(Guid id)
        {
            try
            {
                var request = await context.AddDriverRequests.FindAsync(id);
                if (request is null) return Error.NotFound(description: "Request not found");

                await context.Drivers.AddAsync(new Driver
                {
                    Name = request.Name,
                    VehicleNo = request.VehicleNo,
                    Contact = request.Contact,
                    TanksInTruck = request.TanksInTruck,
                    IsActive = true,
                    DateCreated = DateTime.UtcNow
                });

                context.AddDriverRequests.Remove(request);
                await context.SaveChangesAsync();

                return new SuccessResponse { Message = "Request approved successfully" };
            }
            catch (Exception ex)
            {
                return Error.Failure(description: ex.Message);
            }
        }

        public async Task<ErrorOr<SuccessResponse>> RejectDriverRequestAsync(Guid id)
        {
            try
            {
                var request = await context.AddDriverRequests.FindAsync(id);
                if (request is null) return Error.NotFound(description: "Request not found");

                context.AddDriverRequests.Remove(request);
                await context.SaveChangesAsync();

                return new SuccessResponse { Message = "Request rejected" };
            }
            catch (Exception ex)
            {
                return Error.Failure(description: ex.Message);
            }
        }
    }

    public class DriverSummaryDto
    {
        [JsonProperty("id")]
        public Guid Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; } = null!;

        [JsonProperty("regNo")]
        public string RegNo { get; set; } = null!;

        [JsonProperty("contact")]
        public string Contact { get; set; } = null!;

        [JsonProperty("tanksInTruck")]
        public int TanksInTruck { get; set; }
    }

    public class DriverRequestDto
    {
        [JsonProperty("id")]
        public Guid Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; } = null!;

        [JsonProperty("regNo")]
        public string RegNo { get; set; } = null!;

        [JsonProperty("contact")]
        public string Contact { get; set; } = null!;

        [JsonProperty("tanksInTruck")]
        public int TanksInTruck { get; set; }

        [JsonProperty("requestedByName")]
        public string? RequestedByName { get; set; }

        [JsonProperty("dateRequested")]
        public DateTime DateRequested { get; set; }
    }
}