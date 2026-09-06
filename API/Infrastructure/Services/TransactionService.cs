using System.Text;
using ClosedXML.Excel;
using ErrorOr;
using API.Controllers;
using API.Infrastructure.Persistence;
using API.Models;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using static API.Infrastructure.Services.Common;

namespace API.Infrastructure.Services
{
    public class TransactionService(AppDbContext context)
    {
        public async Task<ErrorOr<SuccessResponse>> AddTxnAsync(SellDto model, string processedByUserId)
        {
            try
            {
                var user = await context.Users.FindAsync(processedByUserId);
                if (user?.ShopId is null) return Error.Validation(description: "Your account isn't assigned to a shop yet");

                await context.Transactions.AddAsync(new Transaction
                {
                    Container = model.ContainerType == "TANK" ? ContainerType.TANK : ContainerType.BUCKET,
                    Buyer = model.Buyer,
                    Quantity = model.Quantity,
                    UnitPrice = model.UnitPrice,
                    TotalAmount = model.UnitPrice * model.Quantity,
                    ShopId = user.ShopId.Value,
                    ProcessedByUserId = processedByUserId,
                    PaymentMethod = model.PaymentMethod,
                    VehicleNo = model.VehicleNo,
                    DateCreated = DateTime.UtcNow
                });

                var rowsAffected = await context.SaveChangesAsync();
                if (rowsAffected > 0) return new SuccessResponse { Message = "Transaction recorded successfully" };

                return Error.Failure(description: "Error adding transaction");
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

                var totalRecords = await context.Transactions.CountAsync();
                var totalQuantity = await context.Transactions.SumAsync(t => (int?)t.Quantity) ?? 0;
                var totalCollected = await context.Transactions.SumAsync(t => (decimal?)t.TotalAmount) ?? 0;
                var collectedToday = await context.Transactions
                    .Where(t => t.DateCreated >= today && t.DateCreated < tomorrow)
                    .SumAsync(t => (decimal?)t.TotalAmount) ?? 0;

                return new TransactionsSummaryDto
                {
                    TotalRecords = totalRecords,
                    TotalQuantity = totalQuantity,
                    TotalCollected = totalCollected,
                    CollectedToday = collectedToday
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
                var query = context.Transactions.AsQueryable();

                if (date.HasValue)
                {
                    // Query-string DateTime binding produces Kind=Unspecified, but the
                    // DateCreated column is timestamptz — Npgsql refuses to compare
                    // against an Unspecified-kind value, so it must be marked UTC first.
                    var day = DateTime.SpecifyKind(date.Value.Date, DateTimeKind.Utc);
                    query = query.Where(t => t.DateCreated >= day && t.DateCreated < day.AddDays(1));
                }

                if (!string.IsNullOrWhiteSpace(paymentMethod))
                {
                    query = query.Where(t => t.PaymentMethod == paymentMethod);
                }

                var records = await query
                    .OrderByDescending(t => t.DateCreated)
                    .Select(t => new TransactionRecordDto
                    {
                        Id = t.Id,
                        Buyer = t.Buyer ?? "",
                        VehicleNo = t.VehicleNo,
                        PaymentMethod = t.PaymentMethod,
                        TotalAmount = t.TotalAmount,
                        ContainerType = t.Container.ToString(),
                        Quantity = t.Quantity,
                        DateCreated = t.DateCreated
                    })
                    .ToListAsync();

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
