using HoneyErpAPI.Common;
using System.Text.Json.Serialization;
using static WebApplication1.Common.Models;

namespace HoneyErpAPI.Models
{
    // ── Raw Material List ─────────────────────────────────────────────────────
    public class GetRawMaterialListReq : BaseApiRequest
    {
        public PaginationRequest Pagenation { get; set; }
        public string? SearchKeyword { get; set; }
        public string? Category { get; set; }
        public StockStatus? Status { get; set; }
    }


    public class RawMaterialListItem
    {
        public string  Edid { get; set; } = string.Empty;
        public string  Name { get; set; } = string.Empty;
        public string  Category { get; set; } = string.Empty;
        public string  UnitEdid { get; set; } = string.Empty;
        public decimal Stock { get; set; }
        public decimal MinQuantity  { get; set; }
        public decimal ReorderLevel { get; set; }
        public string AllowedUnitEdid   { get; set; } = string.Empty;
        public string AllowedUnitSymbol { get; set; } = string.Empty;
        public StockStatus Status { get; set; }
    }

    public class GetPackingMaterialsRes : BaseApiResponse
    {
        public List<PackingMaterialItemDto> Items { get; set; } = new();
    }

    public class PackingMaterialItemDto
    {
        public string Edid { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public decimal Stock { get; set; }
        public string UnitSymbol { get; set; } = string.Empty;
        public string? UnitEdid { get; set; }
    }

    public class GetRawMaterialByIdRes : BaseApiResponse
    {
        public string  Edid         { get; set; } = string.Empty;
        public string  Name         { get; set; } = string.Empty;
        public string  Category     { get; set; } = string.Empty;
        public string  UnitEdid     { get; set; } = string.Empty;
        public decimal Stock        { get; set; }
        public decimal MinQuantity  { get; set; }
        public decimal ReorderLevel { get; set; }
        public StockStatus Status { get; set; }
    }

    public class AddRawMaterialReq : BaseApiRequest
    {
        public string Name { get; set; } = string.Empty;
        public string CategoryEdid { get; set; } = string.Empty;
        public string  UnitEdid { get; set; } = string.Empty;
        public decimal Stock { get; set; }
        public decimal MinQuantity { get; set; }
        public decimal ReorderLevel { get; set; }
    }

    public class AddRawMaterialRes : BaseApiResponse
    {
        public string Edid { get; set; } = string.Empty;
    }

    public class EditRawMaterialReq : BaseApiRequest
    {
        public string  Name         { get; set; } = string.Empty;
        public string  Category     { get; set; } = string.Empty;
        public string  UnitEdid     { get; set; } = string.Empty;
        public decimal Stock        { get; set; }
        public decimal MinQuantity  { get; set; }
        public decimal ReorderLevel { get; set; }
    }

    public class EditRawMaterialRes : BaseApiResponse { }

    // ── Recipe ────────────────────────────────────────────────────────────────
    public class GetRecipeRes : BaseApiResponse
    {
        public List<RecipeItemLine> Items { get; set; } = new();
    }

    public class RecipeItemLine
    {
        public int     RecipeItemId      { get; set; }
        public string  RecipeItemEdid    { get; set; } = string.Empty;
        public string  MaterialEdid      { get; set; } = string.Empty;
        public string  MaterialName      { get; set; } = string.Empty;
        public string  Category          { get; set; } = string.Empty;
        public string  AllowedUnitSymbol { get; set; } = string.Empty;
        public decimal Qty               { get; set; }
    }

    public class SaveRecipeReq : BaseApiRequest
    {
        public List<SaveRecipeItemLine> Items { get; set; } = new();
    }

    public class SaveRecipeItemLine
    {
        public string  MaterialEdid { get; set; } = string.Empty;
        public decimal Qty          { get; set; }
    }

    public class ToggleRecipeItemStatusReq : BaseApiRequest
    {
        public bool Active { get; set; }
    }
    public class ToggleRecipeItemStatusRes : BaseApiResponse { }

    public class SaveRecipeRes : BaseApiResponse { }

    public class GetCategoryListReq : BaseApiRequest
    {
        public PaginationRequest Pagination { get; set; } = new PaginationRequest();
        public string? Name   { get; set; }
    }

    public class CategoryListItem
    {
        public string Edid              { get; set; } = string.Empty;
        public string Name              { get; set; } = string.Empty;
        public string AllowedUnitEdid   { get; set; } = string.Empty;
        public string AllowedUnitSymbol { get; set; } = string.Empty;
    }

    public class AddCategoryReq : BaseApiRequest
    {
        public string Name { get; set; } = string.Empty;
        public string AllowedUnitEdid { get; set; } = string.Empty;
    }

    public class AddCategoryRes : BaseApiResponse
    {
        public string Edid { get; set; } = string.Empty;
    }

    public class GetMaterialPurchaseHistoryReq
    {
        public PaginationRequest Pagination { get; set; } = new PaginationRequest();
    }

    public class MaterialPurchaseHistoryItem
    {
        public string PurchaseNumber { get; set; } = string.Empty;
        public string PurchaseEdid { get; set; } = string.Empty;
        public List<AttachmentModuleRes> AttachmentData { get; set; } = new();
        public string SupplierName { get; set; } = string.Empty;
        public string BatchNumber { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public DateTime PurchaseDate { get; set; }
    }

    public class GetMaterialUsageHistoryReq
    {
        [JsonIgnore]
        public string? MaterialEdid { get; set; }
        [JsonIgnore]
        public string? MaterialBatchEdid { get; set; }
    }



    public class MaterialUsageHistoryRes:BaseApiResponse
    {
        public List<MaterialUsageHistoryItem> Items { get; set; }
    }

    public class MaterialUsageHistoryItem
    {
        public string Remark { get; set; }
        public decimal QuantityUsed { get; set; }
        public DateTime UsageDate { get; set; }
    }
}
