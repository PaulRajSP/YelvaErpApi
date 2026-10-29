using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using HoneyErpAPI.Common;
using AttachmentEnum;
using static WebApplication1.Common.Models;

namespace HoneyErpAPI.Models
{
    public class OrderPaymentAttachmentReq
    {
        public string? AttachmentEdid { get; set; }
        public OrderPaymentAttachmentType AttachmentType { get; set; } = OrderPaymentAttachmentType.Receipt;
        public string? Remarks { get; set; }
    }

    public class OrderPaymentItem
    {
        public string Edid { get; set; } = string.Empty;
        public string OrderEdid { get; set; } = string.Empty;
        public string OrderNumber { get; set; } = string.Empty;
        public string TransactionNumber { get; set; } = string.Empty;
        public OrderPaymentMethod PaymentMethod { get; set; }
        public OrderPaymentType PaymentType { get; set; }
        public decimal Amount { get; set; }
        public OrderPaymentStatus Status { get; set; }
        public string? PaymentReference { get; set; }
        public DateTime? PaymentDate { get; set; }
        public string? Remark { get; set; }
        public List<AttachmentModuleRes> AttachmentData { get; set; } = new();
        public DateTime CreatedAt { get; set; }
    }

    public class RecordOrderPaymentReq : BaseApiRequest
    {
        public string? OrderEdid { get; set; }
        public decimal Amount { get; set; }
        public OrderPaymentMethod PaymentMethod { get; set; } = OrderPaymentMethod.UPI;
        public OrderPaymentType PaymentType { get; set; } = OrderPaymentType.FullPayment;
        public OrderPaymentStatus Status { get; set; } = OrderPaymentStatus.Success;
        public string? PaymentReference { get; set; }
        public DateTime? PaymentDate { get; set; }
        public string? Remark { get; set; }
        public List<OrderPaymentAttachmentReq>? AttachmentData { get; set; }
        public bool SyncToLedger { get; set; } = true;
    }

    public class RecordOrderPaymentRes : BaseApiResponse
    {
        public string Edid { get; set; } = string.Empty;
        public string TransactionNumber { get; set; } = string.Empty;
        public int OrderId { get; set; }
        public string OrderEdid { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Status { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public decimal AmountPaid { get; set; }
        public decimal BalanceAmount { get; set; }
        public string PaymentStatus { get; set; } = string.Empty;
        public List<AttachmentModuleRes> AttachmentData { get; set; } = new();
    }

    public class GetOrderPaymentsRes : BaseApiResponse
    {
        public string OrderEdid { get; set; } = string.Empty;
        public string OrderNumber { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public decimal AmountPaid { get; set; }
        public decimal BalanceAmount { get; set; }
        public string PaymentStatus { get; set; } = string.Empty;
        public List<OrderPaymentItem> Payments { get; set; } = new();
    }

    public class DeleteOrderPaymentRes : BaseApiResponse
    {
        public decimal TotalAmount { get; set; }
        public decimal AmountPaid { get; set; }
        public decimal BalanceAmount { get; set; }
        public string PaymentStatus { get; set; } = string.Empty;
    }

    public class OrderPaymentSummaryRes : BaseApiResponse
    {
        public int OrderId { get; set; }
        public string OrderEdid { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public decimal AmountPaid { get; set; }
        public decimal BalanceAmount { get; set; }
        public int PaymentCount { get; set; }
        public string PaymentStatus { get; set; } = string.Empty;
    }

    public class OrderHeaderSummary
    {
        public int OrderId { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public string PaymentStatus { get; set; } = string.Empty;
    }

    public class OrderPaymentRecord
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public string TransactionNumber { get; set; } = string.Empty;
        public OrderPaymentMethod PaymentMethod { get; set; }
        public OrderPaymentType PaymentType { get; set; }
        public decimal Amount { get; set; }
        public OrderPaymentStatus Status { get; set; }
        public string? PaymentReference { get; set; }
        public DateTime? PaymentDate { get; set; }
        public string? Remark { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
