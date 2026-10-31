using HoneyErpAPI.Common;
using System.Text.Json.Serialization;
using static WebApplication1.Common.Models;

namespace HoneyErpAPI.Models
{
    // ── Production Request List ───────────────────────────────────────────────
    public class GetProductionRequestListReq : BaseApiRequest
    {
        public PaginationRequest Pagination { get; set; } = new PaginationRequest();
        public ProductionStatus? Status { get; set; }
        public ProductionPriority? Priority { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string? SearchKeyword { get; set; }
    }

    public class ProductionRequestListItem
    {
        public string   Edid                { get; set; } = string.Empty;
        public string   RequestNumber       { get; set; } = string.Empty;
        public string   ProductEdid         { get; set; } = string.Empty;
        public string?  ProductName         { get; set; }
        public string?  ProductSku          { get; set; }
        public decimal  RequestedQty        { get; set; }
        public decimal  CompletedQty        { get; set; }
        public ProductionStatus Status      { get; set; }
        public ProductionPriority Priority  { get; set; }
        public string?  Reason              { get; set; }
        public string?  RequestedBy         { get; set; }
        public decimal? ProductionUnitCost  { get; set; }
        public DateTime CreatedAt           { get; set; }
    }

    // ── Get Production Request by ID ──────────────────────────────────────────
    public class GetProductionRequestByIdRes : BaseApiResponse
    {
        public string   Edid                { get; set; } = string.Empty;
        public string   RequestNumber       { get; set; } = string.Empty;
        public string   ProductEdid         { get; set; } = string.Empty;
        public string?  ProductName         { get; set; }
        public string?  ProductSku          { get; set; }
        public decimal? ProductStock        { get; set; }
        public decimal  RequestedQty        { get; set; }
        public decimal  CompletedQty        { get; set; }
        public ProductionStatus Status      { get; set; }
        public ProductionPriority Priority  { get; set; }
        public string?  Reason              { get; set; }
        public string?  RequestedBy         { get; set; }
        public decimal? ProductionUnitCost  { get; set; }   
        public DateTime CreatedAt           { get; set; }
        public List<ProductionRequestMaterialItem> Materials { get; set; } = new();
    }

    public class ProductionRequestMaterialItem
    {
        public string   Edid              { get; set; } = string.Empty;
        public string   MaterialEdid      { get; set; } = string.Empty;
        public string?  MaterialBatchEdid { get; set; }
        public string?  RecipeItemEdid    { get; set; }
        public string?  BatchNumber       { get; set; }
        public string   MaterialName      { get; set; } = string.Empty;
        public string?  Category          { get; set; }
        public string?  UnitSymbol        { get; set; }
        public decimal  RecipeQty         { get; set; }
        public decimal  RequiredQty       { get; set; }
        public decimal  AvailableStock    { get; set; }
        public bool     IsSufficient      { get; set; }
    }

    // ── Create Production Request ─────────────────────────────────────────────
    public class CreateProductionRequestReq : BaseApiRequest
    {
        public string   ProductEdid { get; set; } = string.Empty;
        public decimal  RequestedQty { get; set; }
        public ProductionPriority Priority { get; set; }
        [JsonIgnore]
        public ProductionStatus Status { get; set; }
        public string?  Reason { get; set; }
        public string?  RequestedBy { get; set; }
        public decimal? ProductionUnitCost { get; set; }
        public List<CreateProductionRequestMaterialLine>? Materials { get; set; }
    }

    public class CreateProductionRequestMaterialLine
    {
        public string   MaterialEdid      { get; set; } = string.Empty;
        public string?  MaterialBatchEdid { get; set; }
        public string?  RecipeItemEdid    { get; set; }
    }

    public class CreateProductionRequestRes : BaseApiResponse
    {
        public string Edid          { get; set; } = string.Empty;
        public string RequestNumber { get; set; } = string.Empty;
    }

    // ── Update Production Request Status ──────────────────────────────────────
    public class UpdateProductionRequestStatusReq : BaseApiRequest
    {

        [JsonIgnore]
        public string? Edid { get; set; } = string.Empty;
        public ProductionStatus Status { get; set; }
        public decimal CompletedQty { get; set; }
    }

    public class ProductionRequestActionRes : BaseApiResponse
    {
        
    }

    // ── Production Request Recipe Raw Materials with Batches ─────────────────────
    public class GetRecipeRawMaterialsRes: BaseApiResponse
    {
        public List<ProductionRequestRecipeRawMaterialItem> RawMaterials { get; set; } = new();
    }

    public class ProductionRequestRecipeRawMaterialItem
    {
        public string RecipeItemEdid { get; set; } = string.Empty;
        public string MaterialEdid { get; set; } = string.Empty;
        public string MaterialName { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string AllowedUnitSymbol { get; set; } = string.Empty;
        public decimal? Qty { get; set; }
        public List<ProductionRequestRecipeBatchItem> RecipeBatches { get; set; } = new();
    }

    public class ProductionRequestRecipeBatchItem
    {
        public string Edid { get; set; } = string.Empty;
        public string MaterialEdid { get; set; } = string.Empty;
        public string BatchNumber { get; set; } = string.Empty;
        public decimal? MaxProductionPossibleQty { get; set; }
        public decimal? QtyAvailable { get; set; }
        public bool IsDefault { get; set; }
    }


    public class GetMaterialBatchListReq
    {
        [JsonIgnore]
        public string? materialEdid { get; set; }

        public string Search { get; set; }
        public PaginationRequest Pagination { get; set; } = new PaginationRequest();
    }

    public class GetMaterialBatchListItem
    {
        public string materialBatchEdid { get; set; }
        public string batchNumber { get; set; }
        public DateTime? latestUsageDate { get; set; }
        public int entriesCount { get; set; }
        public decimal totalUsed { get; set; }
    }
}
