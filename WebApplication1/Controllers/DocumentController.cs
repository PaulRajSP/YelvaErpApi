using System;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using HoneyErpAPI.Common;
using HoneyErpAPI.Models;
using HoneyErpAPI.Services;
using HoneyErpAPI.DocumentHandlers;
using DocumentType = HoneyErpAPI.DocumentHandlers.DocumentType;

namespace HoneyErpAPI.Controllers
{
    [ApiController]
    [Route("api/document")]
    [Route("api/documents")]
    public class DocumentController : Controller
    {
        private readonly ILogger<DocumentController> _logger;
        private readonly DocumentHandlerFactory _handlerFactory;
        private readonly DocumentService _documentService;

        public DocumentController(
            ILogger<DocumentController> logger,
            DocumentHandlerFactory handlerFactory,
            DocumentService documentService)
        {
            _logger = logger;
            _handlerFactory = handlerFactory;
            _documentService = documentService;
        }

        [HttpGet("deliveryslip/{orderNumber}")]
        public IActionResult GenerateDeliverySlip(string orderNumber)
        {
            var handler = (IDeliverySlipHandler)_handlerFactory.GetHandler(DocumentType.DeliverySlip);
            string html = handler.DeliverySlip(orderNumber);
            return Content(html, "text/html");
        }

        [HttpGet("invoice/{orderNumber}")]
        public IActionResult GenerateInvoice(string orderNumber)
        {
            var handler = (IInvoiceHandler)_handlerFactory.GetHandler(DocumentType.Invoice);
            string html = handler.Invoice(orderNumber);
            return Content(html, "text/html");
        }

        [HttpGet("receipt/{orderNumber}")]
        public IActionResult GenerateOrderReceipt(string orderNumber)
        {
            var handler = (IOrderReceiptHandler)_handlerFactory.GetHandler(DocumentType.OrderReceipt);
            string html = handler.OrderReceipt(orderNumber);
            return Content(html, "text/html");
        }

        [HttpGet("confirmed")]
        public IActionResult GenerateConfirmedOrdersReport()
        {
            var handler = (IConfirmedOrdersHandler)_handlerFactory.GetHandler(DocumentType.ConfirmedOrders);
            string html = handler.ConfirmedOrders();
            return Content(html, "text/html");
        }

        [HttpGet("{type}/{identifier}/html")]
        public IActionResult GetDocumentHtml(DocumentType type, string identifier)
        {
            string html = type switch
            {
                DocumentType.DeliverySlip => ((IDeliverySlipHandler)_handlerFactory.GetHandler(type)).DeliverySlip(identifier),
                DocumentType.Invoice => ((IInvoiceHandler)_handlerFactory.GetHandler(type)).Invoice(identifier),
                DocumentType.OrderReceipt => ((IOrderReceiptHandler)_handlerFactory.GetHandler(type)).OrderReceipt(identifier),
                DocumentType.ConfirmedOrders => ((IConfirmedOrdersHandler)_handlerFactory.GetHandler(type)).ConfirmedOrders(),
                _ => string.Empty
            };
            return Content(html, "text/html");
        }

        [HttpGet("confirmed/download")]
        public IActionResult DownloadConfirmedDocuments()
        {
            var handler = (IConfirmedOrdersHandler)_handlerFactory.GetHandler(DocumentType.ConfirmedOrders);
            string html = handler.ConfirmedOrders();
            byte[] fileBytes = Encoding.UTF8.GetBytes(html);
            return File(fileBytes, "text/html", $"Confirmed_DeliverySlips_{DateTime.Now:yyyyMMdd_HHmm}.html");
        }
    }
}