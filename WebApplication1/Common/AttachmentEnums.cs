using System.Text.Json.Serialization;

namespace AttachmentEnum
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum AttachmentModule
    { Order, Courier, Expense, Purchase, OrderPayment, OrderHandover, PurchasePayment }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum CourierAttachmentType
    { Agreement, Receipt, Other }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum OrderPaymentAttachmentType
    { Receipt, PaymentProof, Other }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum OrderHandoverAttachmentType
    { CourierSlip, Receipt, Other }

}
