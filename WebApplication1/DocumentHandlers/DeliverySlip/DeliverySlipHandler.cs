using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using HoneyErpAPI.Common;
using HoneyErpAPI.Models;
using HoneyErpAPI.Repository;
using HoneyErpAPI.Services;

namespace HoneyErpAPI.DocumentHandlers.DeliverySlip
{
    public class DeliverySlipHandler : IDeliverySlipHandler
    {
        private readonly DocumentService _documentService;
        private readonly IOrderRepository _orderRepo;
        private readonly IConfiguration _configuration;
        private readonly ICourierRepository _courierRepo;

        public DeliverySlipHandler(
            DocumentService documentService,
            IOrderRepository orderRepo,
            IConfiguration configuration,
            ICourierRepository courierRepo)
        {
            _documentService = documentService;
            _orderRepo = orderRepo;
            _configuration = configuration;
            _courierRepo = courierRepo;
        }

        public class DeliverySlipComboItem
        {
            public int SNo { get; set; }
            public string ProductName { get; set; } = string.Empty;
            public string Sku { get; set; } = string.Empty;
            public decimal QuantityPerCombo { get; set; }
            public decimal TotalQuantity { get; set; }
        }

        public class DeliverySlipItem
        {
            public int SNo { get; set; }
            public string ProductName { get; set; } = string.Empty;
            public string Sku { get; set; } = string.Empty;
            public int Quantity { get; set; }
            public bool IsCombo { get; set; }
            public List<DeliverySlipComboItem> ComboItems { get; set; } = new();
        }

        public class DeliverySlipData
        {
            public string OrderNumber { get; set; } = string.Empty;
            public string RecipientName { get; set; } = string.Empty;
            public string DeliveryAddress { get; set; } = string.Empty;
            public string PinCode { get; set; } = string.Empty;
            public string RecipientMobile { get; set; } = string.Empty;
            public string SecondaryMobile { get; set; } = string.Empty;
            public string PackageDimensions { get; set; } = string.Empty;
            public string PackageWeight { get; set; } = string.Empty;
            public string TotalQuantity { get; set; } = string.Empty;
            public string ContractInfoHtml { get; set; } = string.Empty;
            public string ContractId { get; set; } = string.Empty;
            public string CourierName { get; set; } = string.Empty;
            public string CourierDetailsHtml { get; set; } = string.Empty;
            public string LogoBase64 { get; set; } = string.Empty;
            public List<DeliverySlipItem> Items { get; set; } = new();
            public string ItemsTableRows { get; set; } = string.Empty;
        }

        private string Render(object data, string templateFileName)
        {
            string[] pathsToTry = new[]
            {
                Path.Combine(Directory.GetCurrentDirectory(), "DocumentHandlers", "DeliverySlip", templateFileName),
                Path.Combine(AppContext.BaseDirectory, "DocumentHandlers", "DeliverySlip", templateFileName),
                Path.Combine(Directory.GetCurrentDirectory(), "Templates", "Documents", templateFileName),
                Path.Combine(AppContext.BaseDirectory, "Templates", "Documents", templateFileName)
            };

            string templatePath = pathsToTry.FirstOrDefault(File.Exists) ?? pathsToTry[0];
            string html = File.ReadAllText(templatePath);
            return _documentService.RenderDocument(data, html);
        }

        public string DeliverySlip(string orderNumber)
        {
            GetOrderFullDetailsRes? order = _orderRepo.GetOrderFullDetails(orderNumber, orderNumber);
            if (order == null) return string.Empty;

            OrderCustomerDetail? cust = order.Customer;
            OrderAddressDetail? addr = order.DeliveryAddress;
            OrderPackageDetail? pkg = order.Package;

            IEnumerable<string?> addressParts = new List<string?>
            {
                addr?.HouseStreet,
                addr?.AreaLocality,
                addr?.Landmark,
                addr?.City,
                addr?.District,
                addr?.State
            }.Where(s => !string.IsNullOrWhiteSpace(s));

            string fullAddress = string.Join(", ", addressParts);
            if (string.IsNullOrWhiteSpace(fullAddress))
            {
                fullAddress = "Direct Pickup / Counter Delivery";
            }

            string pinCode = !string.IsNullOrWhiteSpace(addr?.Pincode) ? addr.Pincode.Trim() : "------";

            var itemsList = new List<DeliverySlipItem>();
            StringBuilder itemsBuilder = new StringBuilder();
            int index = 1;
            foreach (OrderDetailItem item in order.Items)
            {
                string productName = string.IsNullOrWhiteSpace(item.ProductNameSnapshot) ? item.ProductName : item.ProductNameSnapshot;
                string sku = !string.IsNullOrWhiteSpace(item.Sku) ? item.Sku : "";

                var comboList = new List<DeliverySlipComboItem>();
                StringBuilder comboBuilder = new StringBuilder();

                if (item.IsCombo && item.ComboItems != null && item.ComboItems.Count > 0)
                {
                    int cIndex = 1;
                    foreach (OrderComboItemDetail c in item.ComboItems)
                    {
                        decimal perComboQty = c.Quantity;
                        decimal totalComboQty = perComboQty * item.Quantity;
                        string cSku = !string.IsNullOrWhiteSpace(c.Sku) ? c.Sku : "-";

                        comboList.Add(new DeliverySlipComboItem
                        {
                            SNo = cIndex,
                            ProductName = c.ProductName,
                            Sku = cSku,
                            QuantityPerCombo = perComboQty,
                            TotalQuantity = totalComboQty
                        });

                        comboBuilder.Append($@"
                            <tr>
                                <td style=""text-align:center;"">{cIndex}</td>
                                <td><strong>{c.ProductName}</strong></td>
                                <td>{cSku}</td>
                                <td style=""text-align:center;"">{perComboQty:0.##}</td>
                                <td style=""text-align:center; font-weight:800;"">{totalComboQty:0.##}</td>
                            </tr>");
                        cIndex++;
                    }
                }

                itemsList.Add(new DeliverySlipItem
                {
                    SNo = index,
                    ProductName = productName,
                    Sku = sku,
                    Quantity = item.Quantity,
                    IsCombo = item.IsCombo,
                    ComboItems = comboList
                });

                string comboHtml = "";
                if (item.IsCombo && comboList.Count > 0)
                {
                    comboHtml = $@"
                        <div class=""combo-box"">
                            <div class=""combo-box-title"">Physical Items (Combo Breakdown)</div>
                            <table class=""combo-subtable"">
                                <thead>
                                    <tr>
                                        <th style=""width:22px; text-align:center;"">#</th>
                                        <th>Product Name</th>
                                        <th style=""width:65px;"">SKU</th>
                                        <th style=""width:55px; text-align:center;"">Per Combo</th>
                                        <th style=""width:55px; text-align:center;"">Total Qty</th>
                                    </tr>
                                </thead>
                                <tbody>
                                    {comboBuilder}
                                </tbody>
                            </table>
                        </div>";
                }

                string comboBadgeHtml = item.IsCombo ? @"<span class=""combo-badge"">COMBO</span>" : "";

                itemsBuilder.Append($@"
                    <tr>
                        <td class=""col-sno"">{index}</td>
                        <td>
                            <div style=""display:flex; align-items:center; gap:6px;"">
                                <strong>{productName}</strong>
                                {comboBadgeHtml}
                            </div>
                            {(string.IsNullOrWhiteSpace(sku) ? "" : $@"<div style=""font-size:7.5pt; color:#4b5563; margin-top:0.5mm;"">SKU: {sku}</div>")}
                            {comboHtml}
                        </td>
                        <td class=""col-qty"">{item.Quantity}</td>
                    </tr>");
                index++;
            }

            // 1. Resolve Courier Name and ID
            string courierName = !string.IsNullOrWhiteSpace(pkg?.Courier) ? pkg.Courier.Trim() : string.Empty;
            int resolvedCourierId = pkg?.CourierId ?? 0;
            if (resolvedCourierId <= 0 && !string.IsNullOrWhiteSpace(pkg?.Courier))
            {
                GetCourierListReq req = new GetCourierListReq
                {
                    OnlyActive = false
                };

                var allCouriers = _courierRepo.GetAll(req)?.Items;
                var matched = allCouriers?.FirstOrDefault(c => c.Name.Equals(pkg.Courier, StringComparison.OrdinalIgnoreCase));
                if (matched != null)
                {
                    resolvedCourierId = matched.Id;
                    courierName = matched.Name;
                }
            }

            if (resolvedCourierId <= 0)
            {
                var firstActive = _courierRepo.GetAll(new GetCourierListReq { OnlyActive = true })?.Items?.FirstOrDefault();
                if (firstActive != null)
                {
                    resolvedCourierId = firstActive.Id;
                    if (string.IsNullOrWhiteSpace(courierName))
                        courierName = firstActive.Name;
                }
            }

            // 2. Fetch metadata for this courier where showInSlip is 1 (completely dynamic, no hardcoded labels)
            List<CourierMetadataItem> slipMetadata = new List<CourierMetadataItem>();
            if (resolvedCourierId > 0)
            {
                var allMeta = _courierRepo.GetMetadataByCourierId(resolvedCourierId);
                if (allMeta != null && allMeta.Count > 0)
                {
                    slipMetadata = allMeta.Where(m => m.ShowInSlip).ToList();
                }
            }

            // 3. Barcode data: tracking number if available, else order number
            string barcodeData = !string.IsNullOrWhiteSpace(pkg?.Tracking) ? pkg.Tracking.Trim() : order.OrderNumber;

            // 4. Build Courier Details & Metadata table dynamically:
            // Courier Name: [courierName]
            // Table for Details to keep it separate and aligned
            StringBuilder detailsBuilder = new StringBuilder();
            detailsBuilder.Append(@"<div style=""display: flex; flex-direction: column; width: 100%;"">");
            if (!string.IsNullOrWhiteSpace(courierName))
            {
                detailsBuilder.Append($@"<div style=""font-size: 8pt; font-weight: 800; color: #1e293b; margin-bottom: 2px;"">Courier Name: <strong style=""color: #000; font-size: 8.5pt;"">{courierName}</strong></div>");
            }

            if (slipMetadata.Count > 0)
            {
                detailsBuilder.Append(@"<table style=""width: 100%; border-collapse: collapse; font-size: 7.5pt; color: #000; max-width: 120mm;"">");
                detailsBuilder.Append(@"<tbody>");

                // Arrange metadata into a 2-column grid (Label | Value)
                foreach (var sm in slipMetadata)
                {
                    detailsBuilder.Append($@"<tr>");
                    detailsBuilder.Append($@"<td style=""padding: 1px 10px 1px 0; font-size: 7pt; color: #475569; font-weight: 700; text-transform: uppercase; white-space: nowrap; width: 1%;"">{sm.Label}:</td>");
                    detailsBuilder.Append($@"<td style=""padding: 1px 0; font-size: 8pt; color: #000; font-weight: 800; white-space: nowrap;"">{sm.Value}</td>");
                    detailsBuilder.Append($@"</tr>");
                }

                detailsBuilder.Append(@"</tbody></table>");
            }
            detailsBuilder.Append(@"</div>");

            string courierDetailsHtml = detailsBuilder.ToString();
            string contractInfoHtml = $"Order: #{order.OrderNumber}";

            // 1. Fetch the raw Base64 string
            string rawLogoBase64 = _configuration["Shipping:LogoBase64"] ?? "";

            // 2. Format it as a valid Data URI and remove any hidden newlines/spaces
            string logoUri = !string.IsNullOrWhiteSpace(rawLogoBase64)
                ? $"data:image/png;base64,{rawLogoBase64.Replace("\n", "").Replace("\r", "").Replace(" ", "").Trim()}"
                : "";

            var data = new DeliverySlipData
            {
                OrderNumber = order.OrderNumber,
                RecipientName = !string.IsNullOrWhiteSpace(addr?.RecipientName) ? addr.RecipientName : (!string.IsNullOrWhiteSpace(cust?.Name) ? cust.Name : "Valued Customer"),
                DeliveryAddress = fullAddress,
                PinCode = pinCode,
                RecipientMobile = !string.IsNullOrWhiteSpace(addr?.PrimaryMobile) ? addr.PrimaryMobile : (cust?.Mobile ?? "N/A"),
                SecondaryMobile = !string.IsNullOrWhiteSpace(addr?.SecondaryMobile) ? $" / {addr.SecondaryMobile}" : "",
                PackageDimensions = pkg != null && ((pkg.Length ?? 0) > 0 || (pkg.Width ?? 0) > 0 || (pkg.Height ?? 0) > 0)
                                        ? $"{pkg.Length:0.#} x {pkg.Width:0.#} x {pkg.Height:0.#} cm"
                                        : $"L: _______ x W: _______ x H: _______ cm",

                PackageWeight = pkg != null && (pkg.Weight ?? 0) > 0 ? $"{pkg.Weight:0.00} kg" : "0.50 kg",
                TotalQuantity = order.Items.Sum(i => i.Quantity).ToString(),
                ContractInfoHtml = contractInfoHtml,
                ContractId = barcodeData,
                CourierName = courierName,
                CourierDetailsHtml = courierDetailsHtml,
                LogoBase64 = logoUri, // Assign the formatted URI
                Items = itemsList,
                ItemsTableRows = itemsBuilder.ToString()
            };

            return Render(data, "DeliverySlip.html");
        }
    }
}