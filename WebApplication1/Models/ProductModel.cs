using HoneyErpAPI.Common;
using Microsoft.Identity.Client;
using System.Text.Json.Serialization;
using static WebApplication1.Common.Models;

namespace HoneyErpAPI.Models
{
    // ── Product List ──────────────────────────────────────────────────────────
    public class GetProductListReq : BaseApiRequest
    {
        public PaginationRequest Pagination { get; set; } = new PaginationRequest();
        public string? SearchKeyword { get; set; }
        public bool? IsCombo { get; set; }
        public bool? IsActive { get; set; }

    }

    public class GetProductListRes : BaseApiResponse
    {
        public List<ProductListItem> Items      { get; set; } = new();
        public int TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize   { get; set; }
        public int TotalPages => PageSize > 0 ? (int)Math.Ceiling(TotalCount / (double)PageSize) : 0;
    }

    public class ProductListItem
    {
        public string  Edid { get; set; } = string.Empty;
        public string  Name { get; set; } = string.Empty;
        public string? Sku { get; set; }
        public decimal Stock { get; set; }
        public decimal? Qty { get; set; }
        public bool IsActive { get; set; }
        public bool IsCombo { get; set; }
    }

    // ── Get Product by Id ─────────────────────────────────────────────────────
    public class GetProductByIdRes : BaseApiResponse
    {
        public string  Edid { get; set; } = string.Empty;
        public string  Name { get; set; } = string.Empty;
        public string? Sku { get; set; }
        public decimal Stock { get; set; }
        public decimal? Qty { get; set; }
        public bool Active  { get; set; }
        public bool IsCombo { get; set; }
        public string? UnitEdid   { get; set; }
        public string? UnitSymbol { get; set; }
    }

    // ── Add Product ───────────────────────────────────────────────────────────
    public class AddProductReq : BaseApiRequest
    {
        public string  Name { get; set; } = string.Empty;
        [JsonIgnore]
        public string? Sku { get; set; }
        public decimal? Qty { get; set; }
        public bool IsCombo { get; set; }
        public string QrCode { get; set; }
        public List<ComoboItems> ComboItems{ get; set; }
    }

    public class ComoboItems
    {
        public string ProductEdid { get; set; }
        public int Quantity { get; set; }
    }


    public class AddProductRes : BaseApiResponse
    {
        public string ProductEdid { get; set; } = string.Empty;
    }

    public class ToggleActiveReq : BaseApiRequest
    {
        public bool Active { get; set; }
    }

    public class ToggleActiveRes : BaseApiResponse { }

    // ── Combo Items ───────────────────────────────────────────────────────────
    public class GetComboItemsRes : BaseApiResponse
    {
        public List<ComboItemLine> Items { get; set; } = new();
    }

    public class ComboItemLine
    {
        public string  Edid        { get; set; } = string.Empty;
        public string  ProductEdid { get; set; } = string.Empty;
        public string  ProductName { get; set; } = string.Empty;
        public string? Sku         { get; set; }
        public decimal Stock       { get; set; }
        public decimal Quantity    { get; set; }
        public bool    Active      { get; set; }
        public string? UnitSymbol  { get; set; }
        public int     UnitCount   { get; set; }
        public List<PricingLine> Prize { get; set; } = new();
    }

    public class SaveComboItemsReq : BaseApiRequest
    {
        public List<SaveComboItemLine> Items { get; set; } = new();
    }

    public class GetCombosUsingProductReq : BaseApiRequest
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize   { get; set; } = 10;
    }

    public class GetCombosUsingProductRes : BaseApiResponse
    {
        public List<CombosUsingProductItem> Items { get; set; } = new();
        public int TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize   { get; set; }
    }

    public class CombosUsingProductItem
    {
        public string ComboProductEdid { get; set; } = string.Empty;
        public string ComboProductName { get; set; } = string.Empty;
        public bool Active { get; set; }
        public int UnitCount { get; set; }
        public List<ProductPriceItem> Prices { get; set; } = new();
    }

    public class SaveComboItemLine
    {
        public string  ProductEdid { get; set; } = string.Empty;
        public decimal Quantity    { get; set; }
        public string? UnitEdid    { get; set; }
        public int     UnitCount   { get; set; } = 1;
    }

    public class SaveComboItemsRes : BaseApiResponse { }

    // ── Pricing ───────────────────────────────────────────────────────────────
    public class GetPricingRes : BaseApiResponse
    {
        public List<PricingLine> Items { get; set; } = new();
    }

    public class PricingLine
    {
        public string  customerType { get; set; } = string.Empty;
        public List<ProductPriceItem> Price { get; set; } = new();
        public decimal Mrp { get; set; }
    }

    public class SavePricingReq : BaseApiRequest
    {
        public List<SavePricingLine> Items { get; set; } = new();
    }

    public class SavePricingLine
    {
        public CustomerType CustomerType { get; set; }
        public decimal Price { get; set; }
        public decimal Mrp { get; set; }
    }

    public class SavePricingRes : BaseApiResponse { }

    // ── Price History ─────────────────────────────────────────────────────────
    public class GetPriceHistoryReq : BaseApiRequest
    {
        public string ProductEdid { get; set; } = string.Empty;
        public int PageNumber { get; set; } = 1;
        public int PageSize   { get; set; } = 10;
    }

    public class GetPriceHistoryRes : BaseApiResponse
    {
        public List<PriceHistoryGroup> Items { get; set; } = new();
        public int TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize   { get; set; }
        public int TotalPages => PageSize > 0 ? (int)Math.Ceiling(TotalCount / (double)PageSize) : 0;
    }

    public class PriceHistoryGroup
    {
        public string PriceGroupId { get; set; } = string.Empty;
        public DateTime? CreatedAt { get; set; }
        public bool IsActive { get; set; }
        public List<PriceHistoryLine> Details { get; set; } = new();
    }

    public class PriceHistoryLine
    {
        public string  customerType { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public decimal Mrp { get; set; }
    }

    // ── Material Validation ───────────────────────────────────────────────────
    public class ValidateMaterialReq : BaseApiRequest
    {
        public string MaterialEdid { get; set; } = string.Empty;
        public string BatchEdid { get; set; } = string.Empty;
        public string BatchNumber { get; set; } = string.Empty;
        public decimal NewStockValue { get; set; }
    }

    public class ValidateMaterialRes : BaseApiResponse
    {
        public bool IsValid { get; set; }
        public decimal RequiredQty { get; set; }
        public decimal AvailableQty { get; set; }
    }

    // ── Product Reconciliation ─────────────────────────────────────────────────
    public class ReconcileMaterialLine
    {
        public string MaterialEdid      { get; set; } = string.Empty;
        public string MaterialBatchEdid { get; set; } = string.Empty;
        public string BatchNumber       { get; set; } = string.Empty;
        public string RecipeItemEdid    { get; set; } = string.Empty;
        public decimal QuantityUsed     { get; set; }
    }

    public class ReconcileProductBatchReq : BaseApiRequest
    {
        public string ProductEdid      { get; set; } = string.Empty;
        public string? ProductBatchEdid { get; set; }
        public string? BatchNumber      { get; set; }
        public decimal ProductionQty    { get; set; }
        public int? ShelfLifeMonths     { get; set; }
        public DateTime? PackedDate { get; set; }
        [JsonIgnore]
        public DateTime? ExpiryDate { get; set; }
        public string? Reason           { get; set; }
        public string? Remarks          { get; set; }
        public List<ReconcileMaterialLine> Materials { get; set; } = new();
    }

    public class ReconcileProductBatchRes : BaseApiResponse
    {
        public string  ProductBatchEdid { get; set; } = string.Empty;
        public string  BatchNumber      { get; set; } = string.Empty;
        public decimal ProductionQty    { get; set; }
        public decimal RemainingQty     { get; set; }
    }

    // ── Products By Customer (Pricing for Customer Type) ─────────────────────
    public class GetProductsByCustomerReq : BaseApiRequest
    {
        public PaginationRequest Pagination { get; set; }
        
        public string CustomerEdid { get; set; } = string.Empty;
        public string? SearchKeyword { get; set; }
        public bool? IsCombo { get; set; }
    }

    public class CustomerProductListItem
    {
        public string Edid { get; set; } = string.Empty;
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Sku { get; set; }
        public decimal Stock { get; set; }
        public bool IsCombo { get; set; }
        public bool IsActive { get; set; }
        public ProductPriceItem ProductPricing { get; set; }
        public CustomerType? CustomerType { get; set; }
    }


    public class ProductBatchEntry
    {
        public string BatchNumber { get; set; } = string.Empty;
        public string ProductEdid { get; set; } = string.Empty;
        public decimal ProducedQty { get; set; }
        public decimal AvailableQty { get; set; }
        public DateTime PackedDate { get; set; }
        public DateTime ExpiryDate { get; set; }
        public bool IsActive { get; set; } = true;
        public string? Remark { get; set; }
    }
}
