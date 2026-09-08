using System.Text;
using ClosedXML.Excel;
using ErrorOr;
using API.Controllers;
using API.Models;
using Google.Cloud.Firestore;
using Newtonsoft.Json;
using static API.Infrastructure.Services.Common;

namespace API.Infrastructure.Services
{
    public class TransactionService(FirestoreDb db)
    {
        private CollectionReference Transactions => db.Collection("transactions");
        private CollectionReference Users => db.Collection("users");

        public async Task<ErrorOr<SuccessResponse>> AddTxnAsync(SellDto model, string processedByUserId)
        {
            try
            {
                var userSnapshot = await Users.Document(processedByUserId).GetSnapshotAsync();
                var shopId = userSnapshot.Exists ? userSnapshot.GetValue<string?>("ShopId") : null;
                if (string.IsNullOrEmpty(shopId)) return Error.Validation(description: "Your account isn't assigned to a shop yet");

                var id = Guid.NewGuid();
                var totalAmount = model.UnitPrice * model.Quantity;

                await Transactions.Document(id.ToString()).SetAsync(new Dictionary<string, object?>
                {
                    ["Container"] = (model.ContainerType == "TANK" ? ContainerType.TANK : ContainerType.BUCKET).ToString(),
                    ["Buyer"] = model.Buyer,
                    ["Quantity"] = model.Quantity,
                    ["UnitPrice"] = ((decimal)model.UnitPrice).ToString(),
                    ["TotalAmount"] = ((decimal)totalAmount).ToString(),
                    ["ShopId"] = shopId,
                    ["ProcessedByUserId"] = processedByUserId,
                    ["PaymentMethod"] = model.PaymentMethod,
                    ["VehicleNo"] = model.VehicleNo,
                    ["DateCreated"] = DateTime.UtcNow,
                });

                return new SuccessResponse { Message = "Transaction recorded successfully" };
            }
            catch (Exception ex)
            {
                return Error.Failure(description: ex.Message);
            }
        }

        public async Task<ErrorOr<TransactionsSummaryDto>> GetTransactionsSummaryAsync()
        {
            try
            {
                var today = DateTime.UtcNow.Date;
                var tomorrow = today.AddDays(1);

                var all = await Transactions.GetSnapshotAsync();
                var todays = await Transactions
                    .WhereGreaterThanOrEqualTo("DateCreated", today)
                    .WhereLessThan("DateCreated", tomorrow)
                    .GetSnapshotAsync();

                return new TransactionsSummaryDto
                {
                    TotalRecords = all.Count,
                    TotalQuantity = all.Documents.Sum(d => d.GetValue<int>("Quantity")),
                    TotalCollected = all.Documents.Sum(d => decimal.Parse(d.GetValue<string>("TotalAmount"))),
                    CollectedToday = todays.Documents.Sum(d => decimal.Parse(d.GetValue<string>("TotalAmount")))
                };
            }
            catch (Exception ex)
            {
                return Error.Failure(description: ex.Message);
            }
        }

        public async Task<ErrorOr<List<TransactionRecordDto>>> GetTransactionsAsync(DateTime? date, string? paymentMethod)
        {
            try
            {
                Query query = Transactions;

                if (date.HasValue)
                {
                    // DateCreated is stored in UTC; query-string DateTime binding
                    // produces Kind=Unspecified, so mark it UTC before comparing.
                    var day = DateTime.SpecifyKind(date.Value.Date, DateTimeKind.Utc);
                    query = query.WhereGreaterThanOrEqualTo("DateCreated", day).WhereLessThan("DateCreated", day.AddDays(1));
                }

                if (!string.IsNullOrWhiteSpace(paymentMethod))
                {
                    query = query.WhereEqualTo("PaymentMethod", paymentMethod);
                }

                var snapshot = await query.GetSnapshotAsync();

                var records = snapshot.Documents
                    .Select(d => new TransactionRecordDto
                    {
                        Id = Guid.Parse(d.Id),
                        Buyer = d.GetValue<string?>("Buyer") ?? "",
                        VehicleNo = d.GetValue<string?>("VehicleNo"),
                        PaymentMethod = d.GetValue<string>("PaymentMethod"),
                        TotalAmount = decimal.Parse(d.GetValue<string>("TotalAmount")),
                        ContainerType = d.GetValue<string>("Container"),
                        Quantity = d.GetValue<int>("Quantity"),
                        DateCreated = d.GetValue<DateTime>("DateCreated")
                    })
                    .OrderByDescending(r => r.DateCreated)
                    .ToList();

                return records;
            }
            catch (Exception ex)
            {
                return Error.Failure(description: ex.Message);
            }
        }

        public static string BuildCsvExport(List<TransactionRecordDto> records)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Date,Driver/Customer,Vehicle,Type,Quantity,Payment Method,Amount");

            foreach (var r in records)
            {
                sb.AppendLine(
                    $"{r.DateCreated:yyyy-MM-dd HH:mm},{EscapeCsv(r.Buyer)},{EscapeCsv(r.VehicleNo ?? "")},{r.ContainerType},{r.Quantity},{r.PaymentMethod},{r.TotalAmount}"
                );
            }

            return sb.ToString();
        }

        public static byte[] BuildExcelExport(List<TransactionRecordDto> records)
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Transactions");
            var headers = new[] { "Date", "Driver/Customer", "Vehicle", "Type", "Quantity", "Payment Method", "Amount" };

            for (var i = 0; i < headers.Length; i++) worksheet.Cell(1, i + 1).Value = headers[i];

            var row = 2;
            foreach (var r in records)
            {
                worksheet.Cell(row, 1).Value = r.DateCreated.ToString("yyyy-MM-dd HH:mm");
                worksheet.Cell(row, 2).Value = r.Buyer;
                worksheet.Cell(row, 3).Value = r.VehicleNo ?? "";
                worksheet.Cell(row, 4).Value = r.ContainerType;
                worksheet.Cell(row, 5).Value = r.Quantity;
                worksheet.Cell(row, 6).Value = r.PaymentMethod;
                worksheet.Cell(row, 7).Value = r.TotalAmount;
                row++;
            }

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }

        private static string EscapeCsv(string value)
        {
            if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
            {
                return "\"" + value.Replace("\"", "\"\"") + "\"";
            }

            return value;
        }
    }

    public class TransactionsSummaryDto
    {
        [JsonProperty("totalRecords")]
        public int TotalRecords { get; set; }

        [JsonProperty("totalQuantity")]
        public int TotalQuantity { get; set; }

        [JsonProperty("totalCollected")]
        public decimal TotalCollected { get; set; }

        [JsonProperty("collectedToday")]
        public decimal CollectedToday { get; set; }
    }

    public class TransactionRecordDto
    {
        [JsonProperty("id")]
        public Guid Id { get; set; }

        [JsonProperty("buyer")]
        public string Buyer { get; set; } = null!;

        [JsonProperty("vehicleNo")]
        public string? VehicleNo { get; set; }

        [JsonProperty("paymentMethod")]
        public string PaymentMethod { get; set; } = null!;

        [JsonProperty("totalAmount")]
        public decimal TotalAmount { get; set; }

        [JsonProperty("containerType")]
        public string ContainerType { get; set; } = null!;

        [JsonProperty("quantity")]
        public int Quantity { get; set; }

        [JsonProperty("dateCreated")]
        public DateTime DateCreated { get; set; }
    }
}
