using HoneyErpAPI.Common;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using static WebApplication1.Common.Models;

namespace HoneyErpAPI.Models
{
    // ── Product Batches List ────────────────────────────────────────────────
    public class GetProductBatchListReq
    {
        [JsonIgnore]
        public string? ProductEdid { get; set; }
        public string? Search { get; set; }
        public PaginationRequest Pagination { get; set; } = new PaginationRequest();
    }

    public class ProductBatchListItem
    {
        public string ProductBatchEdid { get; set; } = string.Empty;
        public string BatchNumber { get; set; } = string.Empty;
        public string? ProductionNumber { get; set; }
        public string? ProductionEdid { get; set; }
        public decimal ProductionQty { get; set; }
        public decimal RemainingQty { get; set; }
        public bool IsActive { get; set; }
        public string? Remarks { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LatestUsageDate { get; set; }
        public int EntriesCount { get; set; }
        public decimal TotalUsed { get; set; }
    }

    // ── Product Usage History ────────────────────────────────────────────────
    public class GetProductUsageHistoryReq
    {
        [JsonIgnore]
        public string? ProductEdid { get; set; }
        [JsonIgnore]
        public string? ProductBatchEdid { get; set; }
        public string? SearchRemark { get; set; }
    }

    public class ProductUsageHistoryRes : BaseApiResponse
    {
        public List<ProductUsageHistoryItem> Items { get; set; } = new();
    }

    public class ProductUsageHistoryItem
    {
        public string Remark { get; set; } = string.Empty;
        public decimal QuantityUsed { get; set; }
        public DateTime UsageDate { get; set; }
    }
}
