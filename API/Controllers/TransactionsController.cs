using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using API.Infrastructure.Services;
using API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace API.Controllers
{
    [Route("api/[controller]")]
    public class TransactionsController(TransactionService transactionService) : ParentController
    {
        [HttpPost]
        public async Task<IActionResult> PostTxnAsync(SellDto request)
        {
            var userId = GetUserId();
            var response = await transactionService.AddTxnAsync(request, userId);
            return response.Match(Ok, Problem);
        }

        [Authorize(Roles = "admin")]
        [HttpGet]
        public async Task<IActionResult> GetTransactionsAsync([FromQuery] DateTime? date, [FromQuery] string? paymentMethod)
        {
            var response = await transactionService.GetTransactionsAsync(date, paymentMethod);
            return response.Match(Ok, Problem);
        }

        [Authorize(Roles = "admin")]
        [HttpGet("summary")]
        public async Task<IActionResult> GetSummaryAsync()
        {
            var response = await transactionService.GetTransactionsSummaryAsync();
            return response.Match(Ok, Problem);
        }

        [Authorize(Roles = "admin")]
        [HttpGet("export")]
        public async Task<IActionResult> ExportAsync(
            [FromQuery] DateTime? date,
            [FromQuery] string? paymentMethod,
            [FromQuery] string format = "csv")
        {
            var response = await transactionService.GetTransactionsAsync(date, paymentMethod);
            return response.Match(records => BuildExportFile(records, format), Problem);
        }

        private IActionResult BuildExportFile(List<TransactionRecordDto> records, string format)
        {
            if (format == "xlsx")
            {
                var bytes = TransactionService.BuildExcelExport(records);
                return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "transactions.xlsx");
            }

            var csv = TransactionService.BuildCsvExport(records);
            return File(System.Text.Encoding.UTF8.GetBytes(csv), "text/csv", "transactions.csv");
        }
    }

    public class SellDto
    {
        [JsonProperty("buyer")]
        public required string Buyer { get; set; } = string.Empty;

        [JsonProperty("unitPrice")]
        public required int UnitPrice { get; set; }

        [JsonProperty("quantity")]
        public required int Quantity { get; set; }

        [JsonProperty("containerType")]
        [AllowedValues(["TANK", "BUCKET"])]
        public required string ContainerType { get; set; }

        [AllowedValues(["cash", "momo"])]
        [JsonProperty("paymentMethod")]
        public required string PaymentMethod { get; set; }

        [JsonProperty("vehicleNo")]
        public string? VehicleNo { get; set; }
    }
}