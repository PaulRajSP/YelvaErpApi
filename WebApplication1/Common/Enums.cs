using System.Text.Json.Serialization;

namespace HoneyErpAPI.Common
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum StockStatus{InStock,LowStock,OutOfStock}

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum ProductionStatus{Pending,Approved,InProgress,Completed,Cancelled}

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum ProductionPriority{Low,Medium,High,Urgent}

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum OrderStatus{OrderPlaced,Confirmed,Packing,ReadyToShip,Shipped,Delivered,Cancelled}

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum PaymentStatus{Unpaid,PartiallyPaid,Paid}

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum OrderPaymentMethod{UPI,BankTransfer,Cash,Card,Gateway}

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum OrderPaymentType{Advance,FullPayment,Balance}

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum OrderPaymentStatus{Pending,Success,Failed,Cancelled,Refunded}

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum TransactionType{Credit,Debit}

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum TransactionCategory{Sale,Purchase,Expense,Refund,TopUp,Adjustment}

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum OrderProductItemStatus{Pending,Packed,Cancelled}

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum PackageStatus{Pending,Packed,Shipped,Delivered,Cancelled}

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum ExpenseCategory{Electricity,Rent,Water,InternetAndTelecom,MaintenanceAndRepairs,FuelAndLogistics,OfficeSupplies,SalaryAndWages,Marketing,Miscellaneous}

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum ExpenseStatus{Paid,Pending,Cancelled}

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum ExpensePaymentMode{UPI,Cash,BankTransfer,Cheque,Card,NetBanking}

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum DocumentType { Invoice, DeliverySlip, OrderReceipt, PurchaseOrder, ConfirmedOrders }
}

