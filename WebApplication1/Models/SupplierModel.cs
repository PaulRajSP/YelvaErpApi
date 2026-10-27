using HoneyErpAPI.Common;
using System.Text.Json.Serialization;
using static WebApplication1.Common.Models;

namespace HoneyErpAPI.Models
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum SupplierType { FARM, PACKAGING, PRINTING, GENERAL }

    // ── List ─────────────────────────────────────────────────────────────────
    public class GetSupplierListReq : BaseApiRequest
    {
        public PaginationRequest Pagination   { get; set; } = new PaginationRequest();
        public string?           Name         { get; set; }
        public string?           ContactName  { get; set; }
        public SupplierType?     SupplierType { get; set; }
    }

    public class SupplierListItem
    {
        public string  Edid        { get; set; } = string.Empty;
        public string  Name        { get; set; } = string.Empty;
        public string? Type        { get; set; }
        public string? ContactName { get; set; }
        public string? Mobile      { get; set; }
        public decimal Balance     { get; set; }
    }

    // ── Get by Id ────────────────────────────────────────────────────────────
    public class GetSupplierByIdRes : BaseApiResponse
    {
        public string  Edid        { get; set; } = string.Empty;
        public string  Name        { get; set; } = string.Empty;
        public string? Type        { get; set; }
        public string? ContactName { get; set; }
        public string? Mobile      { get; set; }
        public decimal Balance     { get; set; }
    }

    // ── Save ─────────────────────────────────────────────────────────────────
    public class AddSupplierReq : BaseApiRequest
    {
        public string  Name { get; set; } = string.Empty;
        public string? Type { get; set; }
        public string? ContactName { get; set; }
        public string? Mobile { get; set; }
        public decimal Balance { get; set; }
    }

    public class AddSupplierRes : BaseApiResponse
    {
        public string Edid { get; set; } = string.Empty;
    }

    public class EditSupplierReq : BaseApiRequest
    {
        public string  Name        { get; set; } = string.Empty;
        public string? Type        { get; set; }
        public string? ContactName { get; set; }
        public string? Mobile      { get; set; }
        public decimal Balance     { get; set; }
    }

    public class EditSupplierRes : BaseApiResponse { }
}
