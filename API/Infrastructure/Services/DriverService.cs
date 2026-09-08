using API.Controllers;
using API.Models;
using ClosedXML.Excel;
using ErrorOr;
using Google.Cloud.Firestore;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using static API.Infrastructure.Services.Common;

namespace API.Infrastructure.Services
{
    public class DriverService(FirestoreDb db)
    {
        private CollectionReference Drivers => db.Collection("drivers");
        private CollectionReference DriverRequests => db.Collection("addDriverRequests");
        private CollectionReference Users => db.Collection("users");

        public async Task<ErrorOr<List<DriverSummaryDto>>> GetDriversAsync()
        {
            try
            {
                var snapshot = await Drivers.WhereEqualTo("IsActive", true).GetSnapshotAsync();

                var drivers = snapshot.Documents
                    .Select(d => new DriverSummaryDto
                    {
                        Id = Guid.Parse(d.Id),
                        Name = d.GetValue<string>("Name"),
                        RegNo = d.GetValue<string>("VehicleNo"),
                        Contact = d.GetValue<string>("Contact"),
                        TanksInTruck = d.GetValue<int>("TanksInTruck")
                    })
                    .OrderBy(d => d.Name)
                    .ToList();

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
                var docRef = Drivers.Document(id.ToString());
                var snapshot = await docRef.GetSnapshotAsync();
                if (!snapshot.Exists) return Error.NotFound(description: "Vehicle not found");

                await docRef.UpdateAsync(new Dictionary<string, object>
                {
                    ["TanksInTruck"] = tanksInTruck,
                    ["DateUpdated"] = DateTime.UtcNow,
                });

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
                            Id = Guid.NewGuid(),
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

                // Full-replace semantics, matching the previous EF implementation:
                // wipe the existing fleet list, then insert the uploaded rows.
                var existing = await Drivers.GetSnapshotAsync();
                await RunBatchedAsync(existing.Documents.Select(d => d.Reference), (batch, reference) => batch.Delete(reference));
                await RunBatchedAsync(newDrivers, (batch, driver) => batch.Set(Drivers.Document(driver.Id.ToString()), ToDocument(driver)));

                return new SuccessResponse { Message = $"Replaced fleet list with {newDrivers.Count} vehicles" };
            }
            catch (Exception ex)
            {
                return Error.Failure(description: ex.Message);
            }
        }

        // Firestore batched writes cap out at 500 operations — chunk larger
        // sets so a fleet list bigger than that doesn't silently fail.
        private async Task RunBatchedAsync<T>(IEnumerable<T> items, Action<WriteBatch, T> apply)
        {
            foreach (var chunk in items.Chunk(400))
            {
                if (chunk.Length == 0) continue;

                var batch = db.StartBatch();
                foreach (var item in chunk) apply(batch, item);
                await batch.CommitAsync();
            }
        }

        public async Task<ErrorOr<SuccessResponse>> AddDriverAsync(AddDriverDo model)
        {
            try
            {
                var driver = new Driver
                {
                    Id = Guid.NewGuid(),
                    Contact = model.Contact,
                    DateCreated = DateTime.UtcNow,
                    IsActive = true,
                    Name = model.Name,
                    TanksInTruck = model.TanksInTruck,
                    VehicleNo = model.RegNo
                };

                await Drivers.Document(driver.Id.ToString()).SetAsync(ToDocument(driver));
                return new SuccessResponse { Message = "Driver added successfully" };
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
                var snapshot = await Drivers.WhereEqualTo("VehicleNo", regNo).Limit(1).GetSnapshotAsync();
                var driver = snapshot.Documents.FirstOrDefault();
                if (driver == null) return Error.NotFound(description: "Driver with this vehicle No. not found");

                return new SearchDriverResponse
                {
                    Contact = driver.GetValue<string>("Contact"),
                    Name = driver.GetValue<string>("Name"),
                    RegNo = driver.GetValue<string>("VehicleNo"),
                    TanksInTruck = driver.GetValue<int>("TanksInTruck")
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
                var id = Guid.NewGuid();
                await DriverRequests.Document(id.ToString()).SetAsync(new Dictionary<string, object>
                {
                    ["Name"] = model.Name,
                    ["VehicleNo"] = model.RegNo,
                    ["Contact"] = model.Contact,
                    ["TanksInTruck"] = model.TanksInTruck,
                    ["IsActive"] = true,
                    ["DateRequested"] = DateTime.UtcNow,
                    ["RequestedById"] = requestedById,
                });

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
                var snapshot = await DriverRequests.GetSnapshotAsync();
                var requests = snapshot.Documents.ToList();

                var requesterIds = requests.Select(r => r.GetValue<string>("RequestedById")).Distinct().ToList();
                var requesterNames = new Dictionary<string, string>();
                foreach (var chunk in requesterIds.Chunk(30))
                {
                    if (chunk.Length == 0) continue;
                    var refs = chunk.Select(id => Users.Document(id));
                    var userSnapshots = await db.GetAllSnapshotsAsync(refs.ToList());
                    foreach (var userSnapshot in userSnapshots)
                    {
                        if (userSnapshot.Exists) requesterNames[userSnapshot.Id] = userSnapshot.GetValue<string?>("Fullname") ?? "";
                    }
                }

                var result = requests
                    .Select(r => new DriverRequestDto
                    {
                        Id = Guid.Parse(r.Id),
                        Name = r.GetValue<string>("Name"),
                        RegNo = r.GetValue<string>("VehicleNo"),
                        Contact = r.GetValue<string>("Contact"),
                        TanksInTruck = r.GetValue<int>("TanksInTruck"),
                        RequestedByName = requesterNames.GetValueOrDefault(r.GetValue<string>("RequestedById")),
                        DateRequested = r.GetValue<DateTime>("DateRequested")
                    })
                    .OrderBy(r => r.DateRequested)
                    .ToList();

                return result;
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
                var requestRef = DriverRequests.Document(id.ToString());
                var request = await requestRef.GetSnapshotAsync();
                if (!request.Exists) return Error.NotFound(description: "Request not found");

                var driver = new Driver
                {
                    Id = Guid.NewGuid(),
                    Name = request.GetValue<string>("Name"),
                    VehicleNo = request.GetValue<string>("VehicleNo"),
                    Contact = request.GetValue<string>("Contact"),
                    TanksInTruck = request.GetValue<int>("TanksInTruck"),
                    IsActive = true,
                    DateCreated = DateTime.UtcNow
                };

                await Drivers.Document(driver.Id.ToString()).SetAsync(ToDocument(driver));
                await requestRef.DeleteAsync();

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
                var requestRef = DriverRequests.Document(id.ToString());
                var request = await requestRef.GetSnapshotAsync();
                if (!request.Exists) return Error.NotFound(description: "Request not found");

                await requestRef.DeleteAsync();
                return new SuccessResponse { Message = "Request rejected" };
            }
            catch (Exception ex)
            {
                return Error.Failure(description: ex.Message);
            }
        }

        private static Dictionary<string, object> ToDocument(Driver driver) => new()
        {
            ["Name"] = driver.Name,
            ["VehicleNo"] = driver.VehicleNo,
            ["Contact"] = driver.Contact,
            ["TanksInTruck"] = driver.TanksInTruck,
            ["IsActive"] = driver.IsActive,
            ["DateCreated"] = driver.DateCreated,
            ["DateUpdated"] = driver.DateUpdated,
        };
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
