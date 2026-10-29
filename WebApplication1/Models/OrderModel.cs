using HoneyErpAPI.Common;
using AttachmentEnum;
using System.Text.Json.Serialization;
using static WebApplication1.Common.Models;

namespace HoneyErpAPI.Models
{
    // ── Order List ────────────────────────────────────────────────────────────
    public class GetOrderListReq : BaseApiRequest
    {
        public PaginationRequest Pagination { get; set; } = new PaginationRequest();
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string? Status { get; set; }
        public string? PaymentStatus { get; set; }
        public string? SearchKeyword { get; set; }
    }

    public class OrderListItem
    {
        public string   Edid          { get; set; } = string.Empty;
        public string   OrderNumber   { get; set; } = string.Empty;
        public string   customerEdid  { get; set; } = string.Empty;
        public string?  CustomerName  { get; set; }
        public string?  CustomerMobile { get; set; }
        public string?  RecipientName { get; set; }
        public string?  DeliveryType  { get; set; }
        public decimal  TotalAmount   { get; set; }
        public decimal  AmountPaid    { get; set; }
        public decimal  BalanceAmount { get; set; }
        public string   PaymentStatus { get; set; } = string.Empty;
        public string   Status        { get; set; } = string.Empty;
        public DateTime CreatedAt     { get; set; }
    }

    // ── Get Order by Id ───────────────────────────────────────────────────────
    public class GetOrderByIdRes : BaseApiResponse
    {
        public string   Edid              { get; set; } = string.Empty;
        public string   OrderNumber       { get; set; } = string.Empty;
        public string   customerEdid      { get; set; } = string.Empty;
        public string?  DeliveryType      { get; set; }
        public string?  DeliveryAddressEdid { get; set; }
        public decimal  Subtotal          { get; set; }
        public decimal  DeliveryCharge    { get; set; }
        public decimal  Discount          { get; set; }
        public decimal  TotalAmount       { get; set; }
        public decimal  AmountPaid        { get; set; }
        public decimal  BalanceAmount     { get; set; }
        public string   PaymentStatus     { get; set; } = string.Empty;
        public string   Status            { get; set; } = string.Empty;
        public DateTime CreatedAt         { get; set; }
    }

    // ── Create Order ──────────────────────────────────────────────────────────
    public class CreateOrderReq : BaseApiRequest
    {
        public CreateOrderData      Order { get; set; } = new();
        public List<CreateOrderLine> Items { get; set; } = new();
    }

    public class CreateOrderData
    {
        public string   customerEdid       { get; set; } = string.Empty;
        public string?  DeliveryType       { get; set; }
        public string?  DeliveryAddressEdid { get; set; }
        public decimal  Subtotal           { get; set; }
        public decimal  DeliveryCharge     { get; set; }
        public decimal  Discount           { get; set; }
        public decimal  TotalAmount        { get; set; }
        public decimal? InitialPaymentAmount { get; set; }
        public string?  InitialPaymentMethod { get; set; }
        public string?  InitialPaymentReference { get; set; }
        public string?  InitialPaymentType { get; set; }
        public List<OrderPaymentAttachmentReq>? InitialPaymentAttachmentData { get; set; }
    }

    public class CreateOrderLine
    {
        public string  ProductEdid         { get; set; } = string.Empty;
        public string? ProductNameSnapshot { get; set; }
        public string PriceItemEdid { get; set; }
        public decimal UnitPrice           { get; set; }
        public int     Quantity            { get; set; } = 1;
        public bool    IsCombo             { get; set; }
    }

    public class CreateOrderRes : BaseApiResponse
    {
        public string Edid { get; set; } = string.Empty;
        public string OrderNumber { get; set; } = string.Empty;
        [JsonIgnore]
        public string? InitialPaymentEdid { get; set; }
    }

    // ── Order Items ───────────────────────────────────────────────────────────
    public class GetOrderItemsRes : BaseApiResponse
    {
        public List<OrderItemLine> Items { get; set; } = new();
    }

    public class OrderItemLine
    {
        public string  ProductEdid         { get; set; } = string.Empty;
        public string? ProductNameSnapshot { get; set; }
        public decimal UnitPrice           { get; set; }
        public int     Quantity            { get; set; }
        public bool    IsCombo             { get; set; }
    }

    // ── Order Comments ────────────────────────────────────────────────────────
    public class GetOrderCommentsRes : BaseApiResponse
    {
        public List<OrderCommentItem> Items { get; set; } = new();
    }

    public class OrderCommentItem
    {
        public string   Edid      { get; set; } = string.Empty;
        public string?  Text      { get; set; }
        public string?  Author    { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class AddCommentReq : BaseApiRequest
    {
        public string Text   { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
    }

    public class AddCommentRes : BaseApiResponse
    {
        public string Edid { get; set; } = string.Empty;
    }

    // ── Package ───────────────────────────────────────────────────────────────
    public class GetPackageRes : BaseApiResponse
    {
        public string?        Edid          { get; set; }
        public decimal?       Length        { get; set; }
        public decimal?       Width         { get; set; }
        public decimal?       Height        { get; set; }
        public decimal?       Weight        { get; set; }
        public string?        Courier       { get; set; }
        public int?           CourierId     { get; set; }
        public PackageStatus? Status        { get; set; }
        public string?        Tracking      { get; set; }
        public List<AttachmentModuleRes> AttachmentData { get; set; } = new();
        public string?        HandedOverBy  { get; set; }
        public DateTime?      HandedOverAt  { get; set; }
    }

    public class SavePackageReq : BaseApiRequest
    {
        public string?        Edid          { get; set; }
        public decimal?       Length        { get; set; }
        public decimal?       Width         { get; set; }
        public decimal?       Height        { get; set; }
        public decimal?       Weight        { get; set; }
        public PackageStatus? Status        { get; set; }
    }

    public class SavePackageRes : BaseApiResponse
    {
        public string Edid { get; set; } = string.Empty;
    }

    // ── No-data response ──────────────────────────────────────────────────────
    public class OrderActionRes : BaseApiResponse { }

    // ── Get Order Full Details (Order + Customer + Address + Package + Products) ──
    public class GetOrderFullDetailsRes : BaseApiResponse
    {
        public string   Edid                  { get; set; } = string.Empty;
        public string   OrderNumber           { get; set; } = string.Empty;
        public string?  DeliveryType          { get; set; }
        public decimal  Subtotal              { get; set; }
        public decimal  DeliveryCharge        { get; set; }
        public decimal  Discount              { get; set; }
        public decimal  TotalAmount           { get; set; }
        public decimal  AmountPaid            { get; set; }
        public decimal  BalanceAmount         { get; set; }
        public string   PaymentStatus         { get; set; } = string.Empty;
        public string   Status                { get; set; } = string.Empty;
        public DateTime CreatedAt             { get; set; }
        public int      TotalItemsCount       { get; set; }

        public OrderCustomerDetail? Customer          { get; set; }
        public OrderAddressDetail?  DeliveryAddress   { get; set; }
        public OrderPackageDetail?  Package           { get; set; }
        public List<OrderDetailItem> Items            { get; set; } = new();
        public List<OrderCommentItem> Comments        { get; set; } = new();
        public List<OrderPaymentItem> Payments        { get; set; } = new();
        public List<OrderProductItemDto> ProductItems { get; set; } = new();
    }

    public class OrderStatusTrackingDto
    {
        public string   Edid         { get; set; } = string.Empty;
        public string   Status       { get; set; } = string.Empty;
        public string?  Remarks      { get; set; }
        public string?  ChangedBy    { get; set; }
        public string?  EmployeeName { get; set; }
        public DateTime ChangedAt    { get; set; }
    }

    public class UpdateOrderStatusReq
    {
        public string  Status    { get; set; } = string.Empty;
        public string? Remarks   { get; set; }
        public string? ChangedBy { get; set; }
    }

    public class GetOrderStatusTrackingRes : BaseApiResponse
    {
        public string OrderEdid     { get; set; } = string.Empty;
        public string OrderNumber   { get; set; } = string.Empty;
        public string CurrentStatus { get; set; } = string.Empty;
        public List<OrderStatusTrackingDto> Items { get; set; } = new();
    }

    public class OrderCustomerDetail
    {
        public string  CustomerEdid { get; set; } = string.Empty;
        public string  Name         { get; set; } = string.Empty;
        public string  Mobile       { get; set; } = string.Empty;
        public string? Email        { get; set; }
        public string? Type         { get; set; }
        public int?    TotalOrders  { get; set; }
        public decimal? TotalSpent  { get; set; }
    }

    public class OrderAddressDetail
    {
        public string  AddressEdid          { get; set; } = string.Empty;
        public string? AddressType          { get; set; }
        public string? RecipientName        { get; set; }
        public string? HouseStreet          { get; set; }
        public string? AreaLocality         { get; set; }
        public string? Landmark             { get; set; }
        public string? City                 { get; set; }
        public string? District             { get; set; }
        public string? State                { get; set; }
        public string? Pincode              { get; set; }
        public string? DeliveryInstructions { get; set; }
        public string? PrimaryMobile        { get; set; }
        public string? SecondaryMobile      { get; set; }
    }

    public class OrderPackageDetail
    {
        public string?        Edid            { get; set; }
        public decimal?       Length          { get; set; }
        public decimal?       Width           { get; set; }
        public decimal?       Height          { get; set; }
        public decimal?       Weight          { get; set; }
        public string?        Courier         { get; set; }
        public int?           CourierId       { get; set; }
        public PackageStatus? Status          { get; set; }
        public string?        Tracking        { get; set; }
        public List<AttachmentModuleRes> AttachmentData { get; set; } = new();
        public string?        HandedOverBy    { get; set; }
        public DateTime?      HandedOverAt    { get; set; }
    }

    public class OrderDetailItem
    {
        public string  OrderItemEdid       { get; set; } = string.Empty;
        public string  ProductEdid         { get; set; } = string.Empty;
        public string  ProductName         { get; set; } = string.Empty;
        public string? ProductNameSnapshot { get; set; }
        public string? Sku                 { get; set; }
        public string? Image               { get; set; }
        public decimal UnitPrice           { get; set; }
        public int     Quantity            { get; set; }
        public decimal TotalPrice          { get; set; }
        public decimal Mrp                 { get; set; }
        public decimal Stock               { get; set; }
        public bool    IsActive            { get; set; }
        public bool    IsCombo             { get; set; }
        public string? ProductPricingEdid  { get; set; }
        public List<OrderComboItemDetail> ComboItems { get; set; } = new();
    }

    public class OrderComboItemDetail
    {
        public string  ProductEdid { get; set; } = string.Empty;
        public string  ProductName { get; set; } = string.Empty;
        public string? Sku         { get; set; }
        public decimal Quantity    { get; set; }
        public decimal Stock       { get; set; }
        public bool    Active      { get; set; }
        public string? Image       { get; set; }
    }

    public class OrderProductItemDto
    {
        public string   Edid                { get; set; } = string.Empty;
        public int      Id                  { get; set; }
        public string   OrderProductMstEdid { get; set; } = string.Empty;
        public int      OrderProductMstId   { get; set; }
        public string   ProductSingleEdid   { get; set; } = string.Empty;
        public int      ProductSingleId     { get; set; }
        public string?  ProductBatchEdid    { get; set; }
        public int?     ProductBatchId      { get; set; }
        public OrderProductItemStatus Status { get; set; } = OrderProductItemStatus.Pending;
        public string?  PackedBy            { get; set; }
        public DateTime? PackedAt           { get; set; }

        public string   ProductName         { get; set; } = string.Empty;
        public string?  ProductSku          { get; set; }
        public string?  BatchNumber         { get; set; }
        public string?  OrderItemProductName { get; set; }
    }

    public class GetOrderBottlesRes : BaseApiResponse
    {
        public string OrderEdid   { get; set; } = string.Empty;
        public string OrderNumber { get; set; } = string.Empty;
        public List<OrderProductItemDto> Items { get; set; } = new();
    }

    public class ProductAvailableBatchDto
    {
        public string  Edid         { get; set; } = string.Empty;
        public int     Id           { get; set; }
        public string  ProductEdid  { get; set; } = string.Empty;
        public int     ProductId    { get; set; }
        public string  BatchNumber  { get; set; } = string.Empty;
        public decimal RemainingQty { get; set; }
        public bool    IsActive     { get; set; }
    }

    public class GetOrderPackingDetailsRes: BaseApiResponse
    {
        public string                     OrderEdid        { get; set; } = string.Empty;
        public string                     OrderNumber      { get; set; } = string.Empty;
        public string                     Status           { get; set; } = string.Empty;
        public string?                    CustomerName     { get; set; }
        public string?                    CustomerMobile   { get; set; }
        public string?                    DeliveryType     { get; set; }
        public OrderAddressDetail?        DeliveryAddress  { get; set; }
        public OrderPackageDetail?        Package          { get; set; }
        public List<OrderProductItemDto>  Items            { get; set; } = new();
        public List<ProductAvailableBatchDto> AvailableBatches { get; set; } = new();
        public List<UsedPackingMaterialDto> UsedMaterials  { get; set; } = new();
    }

    public class UsedPackingMaterialDto
    {
        public string MaterialEdid { get; set; } = string.Empty;
        public string MaterialName { get; set; } = string.Empty;
        public string? BatchNumber { get; set; }
        public decimal QuantityUsed { get; set; }
        public DateTime? CreatedAt { get; set; }
    }

    public class ConfirmPackingReq : BaseApiRequest
    {
        public string OrderEdid           { get; set; } = string.Empty;
        public string? PackedBy           { get; set; }
        public string? BoxMaterialEdid    { get; set; }
        public decimal? Length            { get; set; }
        public decimal? Width             { get; set; }
        public decimal? Height            { get; set; }
        public decimal? Weight            { get; set; }
        public List<BottlePackingAssignment> BottleAssignments { get; set; } = new();
        public List<PackingMaterialUsageItem> PackingMaterials { get; set; } = new();
    }

    public class PackingMaterialUsageItem
    {
        public string MaterialEdid { get; set; } = string.Empty;
        public decimal Quantity { get; set; } = 1;
    }

    public class BottlePackingAssignment
    {
        public string OrderProductItemEdid { get; set; } = string.Empty;
        public string ProductBatchEdid     { get; set; } = string.Empty;
        public decimal? Quantity           { get; set; }
    }

    public class DispatchOrderReq : BaseApiRequest
    {
        public string    OrderEdid      { get; set; } = string.Empty;
        public string?   Courier        { get; set; }
        public int?      CourierId      { get; set; }
        public string?   CourierEdid    { get; set; }
        public string?   Tracking       { get; set; }
        public string?   TrackingNumber { get; set; }
        public decimal?  TotalWeight    { get; set; }
        public List<OrderHandoverAttachmentReq>? AttachmentData { get; set; }
        public string?   HandedOverBy   { get; set; }
        public DateTime? HandedOverAt   { get; set; }
        public decimal?  CourierCharge  { get; set; }
    }

    public class OrderHandoverAttachmentReq
    {
        public string? AttachmentEdid { get; set; }
        public OrderHandoverAttachmentType AttachmentType { get; set; } = OrderHandoverAttachmentType.CourierSlip;
        public string? Remarks { get; set; }
    }
}
