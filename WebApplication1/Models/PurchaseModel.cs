using HoneyErpAPI.Common;

using System.Text.Json.Serialization;
using static WebApplication1.Common.Models;

namespace HoneyErpAPI.Models
{
    public enum PurchaseStatus
    {
        Paid,
        Unpaid,
        Partial
    }

    // ── Purchase List ─────────────────────────────────────────────────────────
    public class GetPurchaseListReq : BaseApiRequest
    {
        public PaginationRequest Pagination { get; set; } = new PaginationRequest();
        public string? Status { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string? SearchKeyword { get; set; }
        public string? PublicId { get; set; }
    }

    public class GetPurchasesBySupplierReq : BaseApiRequest
    {
        public string SupplierEdid { get; set; } = string.Empty;
        public int PageNumber { get; set; } = 1;
        public int PageSize   { get; set; } = 10;
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string? PublicId { get; set; }
        public string? PaymentStatus { get; set; }
    }

    public class GetPurchaseListRes : BaseApiResponse
    {
        public List<PurchaseListItem> Items { get; set; } = new();
        public int TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize   { get; set; }
        public int TotalPages => PageSize > 0 ? (int)Math.Ceiling(TotalCount / (double)PageSize) : 0;
    }

    public class PurchaseListItem
    {
        public string Edid { get; set; } = string.Empty;
        public string PublicId { get; set; } = string.Empty;
        public string SupplierEdid { get; set; } = string.Empty;
        public string SupplierName { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal Balance { get; set; }
        public string Status { get; set; } = string.Empty;
        public List<AttachmentModuleRes> AttachmentData { get; set; } = new();
    }

    // ── Get Purchase by Id ────────────────────────────────────────────────────
    public class GetPurchaseByIdRes : BaseApiResponse
    {
        public string Edid { get; set; } = string.Empty;
        public string PublicId { get; set; } = string.Empty;
        public string SupplierEdid { get; set; } = string.Empty;
        public string SupplierName { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal Balance { get; set; }
        public string Status { get; set; } = string.Empty;
        public List<AttachmentModuleRes> AttachmentData { get; set; } = new();
        public List<PurchasePaymentItem> Payments { get; set; } = new();
    }

    public class PurchasePaymentItem
    {
        public string Edid { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string TransactionNumber { get; set; } = string.Empty;
        public string PaymentMethod { get; set; } = string.Empty;
        public OrderPaymentType PaymentType { get; set; } = OrderPaymentType.Advance;
        public OrderPaymentStatus Status { get; set; } = OrderPaymentStatus.Success;
        public string? PaymentReference { get; set; }
        public string? Remarks { get; set; }
        public DateTime PaymentDate { get; set; }
        public List<AttachmentModuleRes> AttachmentData { get; set; } = new();
    }

    // ── Create Purchase ───────────────────────────────────────────────────────
    public class CreatePurchaseReq : BaseApiRequest
    {
        public CreatePurchaseItemData Purchase { get; set; } = new();
        public List<CreatePurchaseItemLine> Items  { get; set; } = new();
    }

    public class CreatePurchaseItemData
    {
        public string SupplierEdid { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public string? InitialPaymentMethod { get; set; }
        public OrderPaymentType InitialPaymentType { get; set; } = OrderPaymentType.Advance;
        public string? InitialPaymentReference { get; set; }
        public string? InitialPaymentRemark { get; set; }
        public List<DocumentAttachmentReq>? InitialPaymentAttachmentData { get; set; }
        public List<DocumentAttachmentReq>? AttachmentData { get; set; }
        [JsonIgnore]
        public PurchaseStatus Status { get; set; } = PurchaseStatus.Unpaid;
        [JsonIgnore]
        public string PublicId { get; set; } = string.Empty;
        [JsonIgnore]
        public Decimal Balance { get; set; }
    }

    public class CreatePurchaseItemLine
    {
        public string  MaterialEdid { get; set; } = string.Empty;
        public decimal Qty { get; set; }
        public decimal Rate { get; set; }
        public decimal Total { get; set; }
        public string? BatchEdid { get; set; }
    }

    public class CreatePurchaseRes : BaseApiResponse
    {
        public string Edid { get; set; } = string.Empty;
        public string? InitialPaymentEdid { get; set; }
        public string? InitialPaymentTransactionNumber { get; set; }
    }

    // ── Purchase Items ────────────────────────────────────────────────────────
    public class GetPurchaseItemsReq : BaseApiRequest
    {
        public string PurchaseEdid { get; set; } = string.Empty;
        public int PageNumber { get; set; } = 1;
        public int PageSize   { get; set; } = 10;
    }
    public class GetPurchaseItemsRes : BaseApiResponse
    {
        public List<PurchaseItemLine> Items { get; set; } = new();
        public int TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize   { get; set; }
        public int TotalPages => PageSize > 0 ? (int)Math.Ceiling(TotalCount / (double)PageSize) : 0;
    }

    public class PurchaseItemLine
    {
        public string  PurchaseEdid  { get; set; } = string.Empty;
        public string  MaterialEdid  { get; set; } = string.Empty;
        public string  MaterialName  { get; set; } = string.Empty;
        public string  CategoryName  { get; set; } = string.Empty;
        public string  UnitSymbol    { get; set; } = string.Empty;
        public decimal Qty           { get; set; }
        public decimal Rate          { get; set; }
        public decimal Total         { get; set; }
        public string? BatchNumber      { get; set; }
    }

    // ── Pay Purchase Due ──────────────────────────────────────────────────────
    public class PayPurchaseDueReq : BaseApiRequest
    {
        public string PurchaseEdid { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public OrderPaymentMethod PaymentMethod { get; set; } = OrderPaymentMethod.Cash;
        public OrderPaymentType PaymentType { get; set; } = OrderPaymentType.Advance;
        public OrderPaymentStatus Status { get; set; } = OrderPaymentStatus.Success;
        public string? PaymentReference { get; set; }
        public string? Remarks { get; set; }
        public DateTime? PaymentDate { get; set; }
        public List<DocumentAttachmentReq>? AttachmentData { get; set; }
    }

    public class PayPurchaseDueRes : BaseApiResponse
    {
        public string PurchaseEdid { get; set; } = string.Empty;
        public string PaymentEdid { get; set; } = string.Empty;
        public string TransactionNumber { get; set; } = string.Empty;
        public List<AttachmentModuleRes> AttachmentData { get; set; } = new();
    }

    // ── No-data response (for status updates) ────────────────────────────────
    public class PurchaseActionRes : BaseApiResponse { }
}
