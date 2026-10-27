using System;

namespace HoneyErpAPI.DocumentHandlers
{
    public class DocumentHandlerFactory
    {
        private readonly IServiceProvider _serviceProvider;

        public DocumentHandlerFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public IDocumentHandler GetHandler(DocumentType documentType)
        {
            return documentType switch
            {
                DocumentType.DeliverySlip => (IDocumentHandler)_serviceProvider.GetService(typeof(DeliverySlip.DeliverySlipHandler))!,
                DocumentType.Invoice => (IDocumentHandler)_serviceProvider.GetService(typeof(Invoice.InvoiceHandler))!,
                DocumentType.OrderReceipt => (IDocumentHandler)_serviceProvider.GetService(typeof(OrderReceipt.OrderReceiptHandler))!,
                DocumentType.ConfirmedOrders => (IDocumentHandler)_serviceProvider.GetService(typeof(ConfirmedOrders.ConfirmedOrdersHandler))!,
                _ => throw new ArgumentOutOfRangeException(nameof(documentType), $"Unsupported document type: {documentType}")
            };
        }
    }
}
