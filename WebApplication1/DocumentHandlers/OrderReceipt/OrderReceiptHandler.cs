using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Globalization;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using HoneyErpAPI.Common;
using HoneyErpAPI.Models;
using HoneyErpAPI.Repository;
using HoneyErpAPI.Services;

namespace HoneyErpAPI.DocumentHandlers.OrderReceipt
{
    public class OrderReceiptHandler : IOrderReceiptHandler
    {
        private readonly DocumentService _documentService;
        private readonly IOrderRepository _orderRepo;
        private readonly IConfiguration _configuration;

        public OrderReceiptHandler(DocumentService documentService, IOrderRepository orderRepo, IConfiguration configuration)
        {
            _documentService = documentService;
            _orderRepo = orderRepo;
            _configuration = configuration;
        }

        public class OrderReceiptData
        {
            public string OrderNumber { get; set; } = string.Empty;
            public string ReceiptDate { get; set; } = string.Empty;

            // Delivery details
            public string RecipientName { get; set; } = string.Empty;
            public string DeliveryAddress { get; set; } = string.Empty;

            public string ItemsTableRows { get; set; } = string.Empty;
            public string TotalAmount { get; set; } = string.Empty;
            public string AmountPaid { get; set; } = string.Empty;
            public bool IsBalancePending { get; set; }
            public string BalanceAmount { get; set; } = string.Empty;
            public string PaymentStatus { get; set; } = string.Empty;
            public string LogoBase64 { get; set; } = string.Empty;
        }

        private string Render(object data, string templateFileName)
        {
            string[] pathsToTry = new[]
            {
                Path.Combine(Directory.GetCurrentDirectory(), "DocumentHandlers", "OrderReceipt", templateFileName),
                Path.Combine(AppContext.BaseDirectory, "DocumentHandlers", "OrderReceipt", templateFileName),
                Path.Combine(Directory.GetCurrentDirectory(), "Templates", "Documents", templateFileName),
                Path.Combine(AppContext.BaseDirectory, "Templates", "Documents", templateFileName)
            };

            string templatePath = pathsToTry.FirstOrDefault(File.Exists) ?? pathsToTry[0];
            string html = File.ReadAllText(templatePath);
            return _documentService.RenderDocument(data, html);
        }

        public string OrderReceipt(string orderNumber)
        {
            GetOrderFullDetailsRes? order = _orderRepo.GetOrderFullDetails(orderNumber, orderNumber);
            if (order == null) return string.Empty;

            CultureInfo inCulture = new CultureInfo("en-IN");

            // Build the Items Table Rows
            StringBuilder itemsBuilder = new StringBuilder();
            int index = 1;
            if (order.Items != null && order.Items.Count > 0)
            {
                foreach (OrderDetailItem item in order.Items)
                {
                    itemsBuilder.Append($@"
                        <tr>
                            <td class=""col-sno"">{index++}</td>
                            <td>
                                <strong>{item.ProductName}</strong>
                                <div style=""font-size:7.5pt; color:#4b5563; margin-top:0.5mm;"">SKU: {item.Sku}</div>
                            </td>
                            <td class=""col-qty"">{item.Quantity}</td>
                            <td class=""col-price"">{item.UnitPrice.ToString("C", inCulture)}</td>
                            <td class=""col-total"">{item.TotalPrice.ToString("C", inCulture)}</td>
                        </tr>");
                }
            }
            else
            {
                itemsBuilder.Append(@"<tr><td colspan=""5"" style=""text-align: center; color: #b91c1c; font-weight: bold; padding: 16px;"">No items found for this order.</td></tr>");
            }

            // Format Address
            OrderAddressDetail? addr = order.DeliveryAddress;
            string fullAddress = string.Join(", ", new[]
            {
                addr?.HouseStreet,
                addr?.AreaLocality,
                addr?.Landmark,
                addr?.City,
                addr?.District,
                addr?.State,
                addr?.Pincode
            }.Where(s => !string.IsNullOrWhiteSpace(s)));

            if (string.IsNullOrWhiteSpace(fullAddress))
            {
                fullAddress = "Direct Pickup / Counter Delivery";
            }

            // Fetch and format Base64 logo
            string rawLogoBase64 = _configuration["Shipping:LogoBase64"] ?? "";
            string logoUri = !string.IsNullOrWhiteSpace(rawLogoBase64)
                ? $"data:image/png;base64,{rawLogoBase64.Replace("\n", "").Replace("\r", "").Replace(" ", "").Trim()}"
                : "";

            var data = new OrderReceiptData
            {
                OrderNumber = order.OrderNumber,
                ReceiptDate = DateTime.Now.ToString("dd MMM yyyy"),

                // Assign Delivery details
                RecipientName = addr?.RecipientName ?? (order.Customer?.Name ?? "Valued Customer"),
                DeliveryAddress = fullAddress,

                ItemsTableRows = itemsBuilder.ToString(),
                TotalAmount = order.TotalAmount.ToString("C", inCulture),
                AmountPaid = order.AmountPaid.ToString("C", inCulture),
                IsBalancePending = order.BalanceAmount > 0,
                BalanceAmount = order.BalanceAmount.ToString("C", inCulture),
                PaymentStatus = order.PaymentStatus,
                LogoBase64 = logoUri
            };

            return Render(data, "OrderReceipt.html");
        }
    }
}