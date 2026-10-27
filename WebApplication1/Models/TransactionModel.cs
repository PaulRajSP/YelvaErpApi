using HoneyErpAPI.Common;
using static WebApplication1.Common.Models;

namespace HoneyErpAPI.Models
{
    // ── List ─────────────────────────────────────────────────────────────────
    public class GetTransactionListReq : BaseApiRequest
    {
        public PaginationRequest Pagination { get; set; } = new PaginationRequest();
    }

    public class TransactionListItem
    {
        public string   Edid        { get; set; } = string.Empty;
        public DateTime Date        { get; set; }
        public string   Type        { get; set; } = string.Empty;
        public string   Category    { get; set; } = string.Empty;
        public decimal  Amount      { get; set; }
        public string?  Description { get; set; }
        public bool     IsActive    { get; set; } = true;
    }

    // ── Get by Id ────────────────────────────────────────────────────────────
    public class GetTransactionByIdRes : BaseApiResponse
    {
        public string   Edid        { get; set; } = string.Empty;
        public DateTime Date        { get; set; }
        public string   Type        { get; set; } = string.Empty;
        public string   Category    { get; set; } = string.Empty;
        public decimal  Amount      { get; set; }
        public string?  Description { get; set; }
        public bool     IsActive    { get; set; } = true;
    }

    // ── Create ────────────────────────────────────────────────────────────────
    public class CreateTransactionReq : BaseApiRequest
    {
        public DateTime Date        { get; set; }
        public string   Type        { get; set; } = string.Empty;
        public string   Category    { get; set; } = string.Empty;
        public decimal  Amount      { get; set; }
        public string?  Description { get; set; }
        public bool     IsActive    { get; set; } = true;
    }

    public class CreateTransactionRes : BaseApiResponse
    {
        public string Edid { get; set; } = string.Empty;
    }
}
