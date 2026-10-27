namespace HoneyErpAPI.DocumentHandlers
{
    public interface IDocumentHandler { }

    public interface IDeliverySlipHandler : IDocumentHandler
    {
        string DeliverySlip(string orderNumber);
    }

    public interface IInvoiceHandler : IDocumentHandler
    {
        string Invoice(string orderNumber);
    }

    public interface IOrderReceiptHandler : IDocumentHandler
    {
        string OrderReceipt(string orderNumber);
    }

    public interface IConfirmedOrdersHandler : IDocumentHandler
    {
        string ConfirmedOrders();
    }
}
