using HoneyErpAPI.Common;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace HoneyErpAPI.Models
{
    public class GetBatchListRes : BaseApiResponse
    {
        public List<BatchListItem> Items { get; set; } = new();
    }

    public class BatchListItem
    {
        public string   Edid         { get; set; } = string.Empty;
        public string   MaterialEdid { get; set; } = string.Empty;
        public string?  SupplierEdid { get; set; }
        public string   BatchNumber  { get; set; } = string.Empty;
        public decimal  QtyPurchased { get; set; }
        public decimal  QtyRemaining { get; set; }
        public decimal  UnitCost     { get; set; }
    }

    public class GetBatchByIdRes : BaseApiResponse
    {
        public string   Edid         { get; set; } = string.Empty;
        public string   MaterialEdid { get; set; } = string.Empty;
        public string?  SupplierEdid { get; set; }
        public string   BatchNumber  { get; set; } = string.Empty;
        public decimal  QtyPurchased { get; set; }
        public decimal  QtyRemaining { get; set; }
        public decimal  UnitCost     { get; set; }
    }

    public class CreateBatchReq : BaseApiRequest
    {
        public string   MaterialEdid { get; set; } = string.Empty;
        public string?  SupplierEdid { get; set; }
        [JsonIgnore]
        public string   BatchNumber  { get; set; } = string.Empty;
        [JsonIgnore]
        public decimal  QtyPurchased { get; set; }
        [JsonIgnore]
        public decimal  QtyRemaining { get; set; }
        [JsonIgnore]
        public decimal UnitCost {  get; set; }
    }

    public class CreateBatchRes : BaseApiResponse
    {
        public string BatchEdid { get; set; } = string.Empty;
        public string BatchNumber { get; set; } = string.Empty;
    }

    public class BatchActionRes : BaseApiResponse { }

    public class MaterialBatchAdjustmentReq : BaseApiRequest
    {
        public MaterialBatchEntryType EntryType { get; set; }
        public decimal Quantity { get; set; }
        public string? Remark { get; set; }
    }
}
