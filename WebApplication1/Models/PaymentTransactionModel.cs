using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using HoneyErpAPI.Common;
using static WebApplication1.Common.Models;

namespace HoneyErpAPI.Models
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum PaymentType { Credit, Debit }

    public class PaymentTransactionMaster
    {
        public string Edid { get; set; } = string.Empty;
        public string TransactionNumber { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public PaymentType Status { get; set; }
        public TransactionCategory Category { get; set; } = TransactionCategory.Adjustment;
        public bool IsActive { get; set; } = true;
        public decimal Balance { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class GetPaymentTransactionListReq : BaseApiRequest
    {
        public PaginationRequest Pagination { get; set; } = new PaginationRequest();
        public PaymentType? Status { get; set; }
        public TransactionCategory? Category { get; set; }
        public bool? IsActive { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string? SearchKeyword { get; set; }
    }

    public class PaymentTransactionListItem
    {
        public string Edid { get; set; } = string.Empty;
        public string TransactionNumber { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public PaymentType? Status { get; set; }
        public TransactionCategory? Category { get; set; }
        public bool IsActive { get; set; } = true;
        public decimal Balance { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class CreatePaymentTransactionReq : BaseApiRequest
    {
        public string TransactionNumber { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public PaymentType Status { get; set; } = PaymentType.Credit;
        public TransactionCategory Category { get; set; } = TransactionCategory.Adjustment;
        public bool IsActive { get; set; } = true;
    }

    public class CreatePaymentTransactionRes : BaseApiResponse
    {
        public string Edid { get; set; } = string.Empty;
    }

    public class GetPaymentTransactionBalanceRes : BaseApiResponse
    {
        public decimal Balance { get; set; }
    }
}
