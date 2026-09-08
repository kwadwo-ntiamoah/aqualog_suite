using API.Controllers;
using API.Models;
using ErrorOr;
using Google.Cloud.Firestore;
using Newtonsoft.Json;
using static API.Infrastructure.Services.Common;

namespace API.Infrastructure.Services
{
    public class ContainerService(FirestoreDb db)
    {
        private CollectionReference Containers => db.Collection("containers");

        public async Task<ErrorOr<List<ContainerSummaryDto>>> GetContainersAsync()
        {
            try
            {
                var snapshot = await Containers.WhereEqualTo("IsActive", true).GetSnapshotAsync();

                var containers = snapshot.Documents
                    .Select(FromDocument)
                    .OrderByDescending(c => c.CreatedDate)
                    .Select(c => new ContainerSummaryDto
                    {
                        Id = c.Id,
                        Type = c.Type.ToString(),
                        DisplayName = c.DisplayName,
                        UnitPrice = c.UnitPrice
                    })
                    .ToList();

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
                var container = new Container
                {
                    Id = Guid.NewGuid(),
                    CreatedDate = DateTime.UtcNow,
                    DisplayName = model.DisplayName,
                    Type = model.Type,
                    UnitPrice = model.UnitPrice,
                    IsActive = true
                };

                await Containers.Document(container.Id.ToString()).SetAsync(ToDocument(container));
                return new SuccessResponse { Message = "Container added successfully" };
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
                var docRef = Containers.Document(containerId.ToString());
                var snapshot = await docRef.GetSnapshotAsync();
                if (!snapshot.Exists) return Error.NotFound(description: "Container not found");

                var isActive = snapshot.GetValue<bool>("IsActive");
                await docRef.UpdateAsync("IsActive", !isActive);

                return new SuccessResponse { Message = "Container updated successfully" };
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
                var docRef = Containers.Document(containerId.ToString());
                var snapshot = await docRef.GetSnapshotAsync();
                if (!snapshot.Exists) return Error.NotFound(description: "Container not found");

                await docRef.UpdateAsync("UnitPrice", model.UnitPrice.ToString());
                return new SuccessResponse { Message = "Container updated successfully" };
            }
            catch (Exception ex)
            {
                return Error.Failure(description: ex.Message);
            }
        }

        private static Container FromDocument(DocumentSnapshot snapshot) => new()
        {
            Id = Guid.Parse(snapshot.Id),
            UnitPrice = decimal.Parse(snapshot.GetValue<string>("UnitPrice")),
            Type = Enum.Parse<ContainerType>(snapshot.GetValue<string>("Type")),
            DisplayName = snapshot.GetValue<string>("DisplayName"),
            CreatedDate = snapshot.GetValue<DateTime>("CreatedDate"),
            IsActive = snapshot.GetValue<bool>("IsActive"),
        };

        private static Dictionary<string, object> ToDocument(Container container) => new()
        {
            ["UnitPrice"] = container.UnitPrice.ToString(),
            ["Type"] = container.Type.ToString(),
            ["DisplayName"] = container.DisplayName,
            ["CreatedDate"] = container.CreatedDate,
            ["IsActive"] = container.IsActive,
        };
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
