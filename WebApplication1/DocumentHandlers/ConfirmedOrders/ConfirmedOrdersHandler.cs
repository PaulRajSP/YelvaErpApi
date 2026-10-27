using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Linq;
using System.Collections.Generic;
using HoneyErpAPI.Common;
using HoneyErpAPI.Repository;
using HoneyErpAPI.Services;

namespace HoneyErpAPI.DocumentHandlers.ConfirmedOrders
{
    public class ConfirmedOrdersHandler : IConfirmedOrdersHandler
    {
        private readonly DocumentService _documentService;
        private readonly IOrderRepository _orderRepo;
        private readonly DeliverySlip.DeliverySlipHandler _deliverySlipHandler;

        public ConfirmedOrdersHandler(
            DocumentService documentService,
            IOrderRepository orderRepo,
            DeliverySlip.DeliverySlipHandler deliverySlipHandler)
        {
            _documentService = documentService;
            _orderRepo = orderRepo;
            _deliverySlipHandler = deliverySlipHandler;
        }

        public class ConfirmedOrdersData
        {
            public int TotalCount { get; set; }
            public string OrderSlipsHtml { get; set; } = string.Empty;
        }

        private string Render(object data, string templateFileName)
        {
            string[] pathsToTry = new[]
            {
                Path.Combine(Directory.GetCurrentDirectory(), "DocumentHandlers", "ConfirmedOrders", templateFileName),
                Path.Combine(AppContext.BaseDirectory, "DocumentHandlers", "ConfirmedOrders", templateFileName),
                Path.Combine(Directory.GetCurrentDirectory(), "Templates", "Documents", templateFileName),
                Path.Combine(AppContext.BaseDirectory, "Templates", "Documents", templateFileName)
            };

            string templatePath = pathsToTry.FirstOrDefault(File.Exists) ?? pathsToTry[0];
            string html = File.ReadAllText(templatePath);
            return _documentService.RenderDocument(data, html);
        }

        public string ConfirmedOrders()
        {
            List<string> confirmedOrderNumbers = _orderRepo.GetOrderNumbersByStatus("Confirmed");
            if (confirmedOrderNumbers == null || confirmedOrderNumbers.Count == 0)
            {
                return @"<!DOCTYPE html><html><body style=""text-align:center;padding:50px;font-family:sans-serif;""><h2>No Confirmed Orders</h2><p>There are no orders in Confirmed status to print.</p></body></html>";
            }

            StringBuilder slipsBuilder = new StringBuilder();
            for (int i = 0; i < confirmedOrderNumbers.Count; i++)
            {
                string orderNumber = confirmedOrderNumbers[i];
                string singleHtml = _deliverySlipHandler.DeliverySlip(orderNumber);
                if (string.IsNullOrWhiteSpace(singleHtml)) continue;

                string innerContent = singleHtml;
                Match match = Regex.Match(singleHtml, @"<body[^>]*>([\s\S]*?)<\/body>", RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    innerContent = match.Groups[1].Value;
                }

                slipsBuilder.Append($@"
                    <div class=""slip-page"">
                        {innerContent}
                    </div>");
            }

            var data = new ConfirmedOrdersData
            {
                TotalCount = confirmedOrderNumbers.Count,
                OrderSlipsHtml = slipsBuilder.ToString()
            };

            return Render(data, "ConfirmedOrders.html");
        }
    }
}
