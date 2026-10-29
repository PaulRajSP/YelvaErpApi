using HoneyErpAPI.Common;
using System.Text.Json.Serialization;
using static WebApplication1.Common.Models;

namespace HoneyErpAPI.Models
{
    public class ExpenseItemDto : BaseApiResponse
    {
        public string   Edid            { get; set; } = string.Empty;
        public string   ExpenseNumber   { get; set; } = string.Empty;
        public string   Title           { get; set; } = string.Empty;
        public string   Category        { get; set; } = string.Empty;
        public decimal  Amount          { get; set; }
        public DateTime ExpenseDate     { get; set; }
        public string   PaymentMode     { get; set; } = string.Empty;
        public string?  PaidTo          { get; set; }
        public string?  ReferenceNumber { get; set; }
        [JsonIgnore]
        public string?  LegacyDocEdid   { get; set; }
        public List<AttachmentModuleRes> AttachmentData { get; set; } = new();
        public string   Status          { get; set; } = string.Empty;
        public string?  Remarks         { get; set; }
        public string?  RecordedBy      { get; set; }
        public int?     TransactionMasterId { get; set; }
        public string?  TransactionMasterEdid { get; set; }
        public DateTime CreatedAt       { get; set; }
        public DateTime UpdatedAt       { get; set; }
    }

    public class GetExpenseListReq : BaseApiRequest
    {
        public PaginationRequest Pagination { get; set; } = new PaginationRequest();
        public DateTime? FromDate       { get; set; }
        public DateTime? ToDate         { get; set; }
        public string?   Category       { get; set; }
        public string?   Status         { get; set; }
        public string?   PaymentMode    { get; set; }
        public string?   SearchKeyword  { get; set; }
    }

    public class CreateExpenseReq : BaseApiRequest
    {
        public string    Title           { get; set; } = string.Empty;
        public string    Category        { get; set; } = string.Empty;
        public decimal   Amount          { get; set; }
        public DateTime? ExpenseDate     { get; set; }
        public string    PaymentMode     { get; set; } = string.Empty;
        public string?   PaidTo          { get; set; }
        public string?   ReferenceNumber { get; set; }
        public List<DocumentAttachmentReq>? AttachmentData { get; set; }
        public string    Status          { get; set; } = string.Empty;
        public string?   Remarks         { get; set; }
        public string?   RecordedBy      { get; set; }
    }

    public class UpdateExpenseReq : BaseApiRequest
    {
        public string    Title           { get; set; } = string.Empty;
        public string    Category        { get; set; } = string.Empty;
        public decimal   Amount          { get; set; }
        public DateTime? ExpenseDate     { get; set; }
        public string    PaymentMode     { get; set; } = string.Empty;
        public string?   PaidTo          { get; set; }
        public string?   ReferenceNumber { get; set; }
        public List<DocumentAttachmentReq>? AttachmentData { get; set; }
        public string    Status          { get; set; } = string.Empty;
        public string?   Remarks         { get; set; }
        public string?   RecordedBy      { get; set; }
    }

    public class CreateExpenseRes : BaseApiResponse
    {
        public string Edid { get; set; } = string.Empty;
    }

    public class UpdateExpenseRes : BaseApiResponse { }

    public class DeleteExpenseRes : BaseApiResponse { }

    public class ExpenseSummaryRes : BaseApiResponse
    {
        public decimal TotalExpenses    { get; set; }
        public decimal ThisMonthTotal   { get; set; }
        public decimal TodayTotal       { get; set; }
        public int     TotalCount       { get; set; }
        public List<CategoryExpenseSummary> CategoryBreakdown { get; set; } = new();
    }

    public class CategoryExpenseSummary
    {
        public string  Category    { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public int     Count       { get; set; }
    }
}
