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

namespace HoneyErpAPI.DocumentHandlers.Invoice
{
    public class InvoiceHandler : IInvoiceHandler
    {
        private readonly DocumentService _documentService;
        private readonly IOrderRepository _orderRepo;
        private readonly IConfiguration _configuration;

        public InvoiceHandler(DocumentService documentService, IOrderRepository orderRepo, IConfiguration configuration)
        {
            _documentService = documentService;
            _orderRepo = orderRepo;
            _configuration = configuration;
        }

        public class InvoiceData
        {
            public string InvoiceNumber { get; set; } = string.Empty;
            public string InvoiceDate { get; set; } = string.Empty;
            public string PaymentStatusBadge { get; set; } = string.Empty;
            public string RecipientName { get; set; } = string.Empty;
            public string RecipientMobile { get; set; } = string.Empty;
            public string DeliveryAddress { get; set; } = string.Empty;
            public string LogoBase64 { get; set; } = string.Empty;
            public string ItemsTableRows { get; set; } = string.Empty;
            public string PaymentsTableRows { get; set; } = string.Empty; // Added for Payment History
            public string Subtotal { get; set; } = string.Empty;
            public bool HasDeliveryCharge { get; set; }
            public string DeliveryCharge { get; set; } = string.Empty;
            public bool HasDiscount { get; set; }
            public string Discount { get; set; } = string.Empty;
            public string TotalAmount { get; set; } = string.Empty;
            public string AmountPaid { get; set; } = string.Empty;
            public bool IsBalancePending { get; set; }
            public string BalanceAmount { get; set; } = string.Empty;
        }

        private string Render(object data, string templateFileName)
        {
            string[] pathsToTry = new[]
            {
                Path.Combine(Directory.GetCurrentDirectory(), "DocumentHandlers", "Invoice", templateFileName),
                Path.Combine(AppContext.BaseDirectory, "DocumentHandlers", "Invoice", templateFileName),
                Path.Combine(Directory.GetCurrentDirectory(), "Templates", "Documents", templateFileName),
                Path.Combine(AppContext.BaseDirectory, "Templates", "Documents", templateFileName)
            };

            string templatePath = pathsToTry.FirstOrDefault(File.Exists) ?? pathsToTry[0];
            string html = File.ReadAllText(templatePath);
            return _documentService.RenderDocument(data, html);
        }

        public string Invoice(string orderNumber)
        {
            GetOrderFullDetailsRes? order = _orderRepo.GetOrderFullDetails(orderNumber, orderNumber);
            if (order == null) return string.Empty;

            CultureInfo inCulture = new CultureInfo("en-IN");

            decimal subtotal = order.Items.Sum(i => i.TotalPrice);
            decimal deliveryCharge = order.DeliveryCharge;
            decimal discount = order.Discount;
            decimal total = order.TotalAmount;
            decimal paid = order.AmountPaid;
            decimal balance = order.BalanceAmount;

            // Build Items Table
            StringBuilder itemsBuilder = new StringBuilder();
            int index = 1;
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

            // Build Payments Table
            StringBuilder paymentsBuilder = new StringBuilder();
            if (order.Payments != null && order.Payments.Count > 0)
            {
                foreach (OrderPaymentItem p in order.Payments)
                {
                    string refText = string.IsNullOrWhiteSpace(p.PaymentReference) ? "Direct" : p.PaymentReference;
                    paymentsBuilder.Append($@"
                        <tr>
                            <td>{(p.PaymentDate.HasValue ? p.PaymentDate.Value.ToString("dd MMM yyyy") : p.CreatedAt.ToString("dd MMM yyyy"))}</td>
                            <td><strong>{p.PaymentMethod}</strong></td>
                            <td style=""color: #4b5563;"">{refText}</td>
                            <td style=""text-align: right; font-weight: 800;"">{p.Amount.ToString("C", inCulture)}</td>
                        </tr>");
                }
            }
            else
            {
                paymentsBuilder.Append(@"<tr><td colspan=""4"" style=""text-align: center; color: #b91c1c; font-weight: bold; padding: 10px;"">No payment transactions recorded yet.</td></tr>");
            }

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

            string paymentBadge = $@"<span style=""background-color: #000000; color: #ffffff; padding: 1mm 2mm; font-size: 8pt; font-weight: 900; text-transform: uppercase; display: inline-block;"">{order.PaymentStatus}</span>";

            string rawLogoBase64 = _configuration["Shipping:LogoBase64"] ?? "";
            string logoUri = !string.IsNullOrWhiteSpace(rawLogoBase64)
                ? $"data:image/png;base64,{rawLogoBase64.Replace("\n", "").Replace("\r", "").Replace(" ", "").Trim()}"
                : "";

            var data = new InvoiceData
            {
                InvoiceNumber = order.OrderNumber,
                InvoiceDate = order.CreatedAt.ToString("dd MMM yyyy"),
                PaymentStatusBadge = paymentBadge,
                RecipientName = addr?.RecipientName ?? "Valued Customer",
                RecipientMobile = addr?.PrimaryMobile ?? "N/A",
                DeliveryAddress = string.IsNullOrWhiteSpace(fullAddress) ? "Direct Pickup / Counter Delivery" : fullAddress,
                LogoBase64 = logoUri,
                ItemsTableRows = itemsBuilder.ToString(),
                PaymentsTableRows = paymentsBuilder.ToString(), // Assign the Payments table
                Subtotal = subtotal.ToString("C", inCulture),
                HasDeliveryCharge = deliveryCharge > 0,
                DeliveryCharge = deliveryCharge.ToString("C", inCulture),
                HasDiscount = discount > 0,
                Discount = discount.ToString("C", inCulture),
                TotalAmount = total.ToString("C", inCulture),
                AmountPaid = paid.ToString("C", inCulture),
                IsBalancePending = balance > 0,
                BalanceAmount = balance.ToString("C", inCulture)
            };

            return Render(data, "Invoice.html");
        }
    }
}